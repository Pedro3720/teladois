using TelaDois.Core.Video;

namespace TelaDois.LatencyBench;

/// <summary>
/// Uma configuração de captura/compressão a medir: perfil de compressão + ajustes + imagens por segundo pedidas ao
/// ddagrab (<c>framerate</c>; o <c>-g</c> segue, para a imagem completa continuar a cada <see cref="EncodeSettings.GopSeconds"/>).
/// </summary>
public sealed record BenchVariant(string Name, string ProfileId, EncodeSettings Settings, ushort Fps = BenchVariant.DefaultFps)
{
    /// <summary>A taxa do app (o monitor virtual é 60 Hz).</summary>
    public const ushort DefaultFps = 60;
}

public static class BenchVariants
{
    /// <summary>
    /// Por que <paramref name="variant"/> não roda numa imagem capturada de <paramref name="width"/>×<paramref name="height"/>,
    /// ou <c>null</c> se roda. O <see cref="EncodeSettings.ScaleTo"/> é absoluto: se não for menor que a captura nas duas
    /// dimensões, a variante ampliaria (ou repetiria) a imagem em vez de reduzir, e a medição não diria nada.
    /// </summary>
    public static string? SkipReason(BenchVariant variant, int width, int height) =>
        variant.Settings.ScaleTo is { } to && (to.Width >= width || to.Height >= height)
            ? $"{variant.Name}: pulada — reduziria para {to.Width}×{to.Height}, que não é menor que a imagem capturada {width}×{height} (ampliaria)"
            : null;

    /// <summary>
    /// As variantes da bancada, na ordem do relatório. <c>base</c> é a linha de hoje do app. Desde 2026-10-21 o perfil
    /// <c>qsv-hwmap</c> já inclui <c>-preset veryfast</c> (provisório), então <c>base</c> e <c>veryfast</c> geram a mesma
    /// linha; <c>veryfast</c> fica (o override troca o valor no lugar) para as medições antigas continuarem comparáveis.
    /// </summary>
    public static IReadOnlyList<BenchVariant> All { get; } =
    [
        new("base", "qsv-hwmap", EncodeSettings.Default),
        new("lowpower", "qsv-hwmap", EncodeSettings.Default with
        {
            CodecOptionOverrides = new Dictionary<string, string> { ["-low_power"] = "1" },
        }),
        new("veryfast", "qsv-hwmap", EncodeSettings.Default with
        {
            CodecOptionOverrides = new Dictionary<string, string> { ["-preset"] = "veryfast" },
        }),
        new("cbr", "qsv-hwmap", EncodeSettings.Default with { Bitrate = "12M", Maxrate = "12M", Bufsize = "200k" }),
        new("intrarefresh", "qsv-hwmap", EncodeSettings.Default with
        {
            ExtraCodecOptions = ["-int_ref_type", "vertical", "-int_ref_cycle_size", "60"],
            GopSeconds = 10,
        }),
        new("gop4", "qsv-hwmap", EncodeSettings.Default with { GopSeconds = 4 }),
        new("nodup", "qsv-hwmap", EncodeSettings.Default with { GrabOptions = ":dup_frames=0" }),
        new("download", "qsv-download", EncodeSettings.Default),
        new("scale1920", "qsv-hwmap", EncodeSettings.Default with { ScaleTo = (1920, 886) }),
        new("scale2of3", "qsv-hwmap", EncodeSettings.Default with { ScaleTo = (1704, 786) }),

        // Rodada 2 (2026-10-21): as duas que mais ajudaram na 1ª matriz, juntas.
        new("veryfast-scale1920", "qsv-hwmap", EncodeSettings.Default with
        {
            CodecOptionOverrides = new Dictionary<string, string> { ["-preset"] = "veryfast" },
            ScaleTo = (1920, 886),
        }),
        new("veryfast-scale2of3", "qsv-hwmap", EncodeSettings.Default with
        {
            CodecOptionOverrides = new Dictionary<string, string> { ["-preset"] = "veryfast" },
            ScaleTo = (1704, 786),
        }),

        // Diagnósticos do parado (~300 ms em todas as variantes QSV da 1ª matriz). x264 e mf trocam só a compressão
        // (mesma captura com download que a variante "download"): se o parado continuar ~300 ms com eles, a demora não é
        // do h264_qsv.
        new("x264", "x264", EncodeSettings.Default),
        new("mf", "mf", EncodeSettings.Default),

        // Diagnóstico do lado da captura: ddagrab a 30/s (o -g cai para 60 = os mesmos 2 s). Hipótese: se o atraso do
        // parado for uma fila de N imagens parada em algum ponto do FFmpeg (captura → filtro → compressão), a 30/s ele
        // dobra (~600 ms); se for um tempo fixo fora do FFmpeg (DWM ou o driver do monitor virtual acordando depois da
        // tela parada), fica nos ~300 ms. Nos cenários com movimento o fps cai para ~30 e quase todo intervalo passa de
        // 33 ms: ali só a mediana e o p95 interessam.
        new("fps30", "qsv-hwmap", EncodeSettings.Default, Fps: 30),

        // Task 5 (2026-10-21): o parado a 60/s (~284 ms; a 30/s o leitor ideal vê em ~30 ms). Rodar só o cenário parado.
        // Cada uma muda uma coisa só em relação à base; FFmpeg 9.0 lido em release/9.0.
        //
        // passthrough / vfr — modo de taxa da saída. O muxer "h264" cru só tem AVFMT_NOTIMESTAMPS
        // (libavformat/rawenc.c, ff_h264_muxer), sem AVFMT_VARIABLE_FPS; então o -fps_mode automático vira CFR, e com
        // 1 fluxo de entrada VSCFR (fftools/ffmpeg_mux_init.c), que repete ou descarta imagens pelos tempos
        // (fftools/ffmpeg_filter.c, video_sync_process). Os tempos do ddagrab são o relógio na hora do pedido
        // (vsrc_ddagrab.c: frame->pts = now; nas repetidas, o tempo antes do AcquireNextFrame, nas novas, depois).
        // passthrough = cada imagem sai com o seu tempo, sem repetir nem descartar; vfr = descarta, mas não repete.
        // Se o parado cair com as duas: era a sincronia CFR; só com passthrough: as repetições; nenhuma: não é ela.
        new("passthrough", "qsv-hwmap", EncodeSettings.Default with { OutputOptions = ["-fps_mode", "passthrough"] }),
        new("vfr", "qsv-hwmap", EncodeSettings.Default with { OutputOptions = ["-fps_mode", "vfr"] }),

        // nobuffer — -fflags nobuffer na entrada: as imagens lidas pelo avformat_find_stream_info ao abrir não ficam
        // guardadas para serem entregues depois (libavformat/demux.c, AVFMT_FLAG_NOBUFFER). Se o parado cair, o atraso
        // era essa sobra do começo. Obs.: -thread_queue_size na entrada não serve no FFmpeg 9 (só o muxer usa,
        // ffmpeg_mux_init.c; a fila demuxer→decodificador é fixa em 8 pacotes, DEFAULT_PACKET_THREAD_QUEUE_SIZE).
        new("nobuffer", "qsv-hwmap", EncodeSettings.Default with { InputOptions = ["-fflags", "nobuffer"] }),

        // graph — ddagrab + filtro do perfil num só -filter_complex (forma documentada em doc/filters.texi, seção
        // ddagrab). Tira o dispositivo lavfi, a sondagem dele, o decodificador wrapped_avframe e a fila de 8 pacotes
        // entre demuxer e decodificador. Se o parado cair para ~30 ms: a demora está na entrada lavfi (e nobuffer diz
        // se é a sobra da sondagem ou a fila); se não mudar: está depois (filtro, compressão, saída). Sem entrada, o
        // -fps_mode automático fica CFR em vez de VSCFR (ffmpeg_mux_init.c: VSCFR exige ost->ist): só muda a 1ª imagem.
        new("graph", "qsv-hwmap", EncodeSettings.Default with { FilterComplex = true }),

        // grab120 — ddagrab a 120/s e fps=60 logo depois (o fps só mexe nos tempos e aceita superfícies D3D11:
        // AVFILTER_FLAG_METADATA_ONLY em libavfilter/vf_fps.c; segura 1 imagem de entrada, ~8 ms). A espera do ddagrab
        // por imagem nova cai de 8 para 4 ms (time_timeout = metade do intervalo) e o pedido acontece 2× por intervalo.
        // Se o parado cair: a demora é do ritmo do ddagrab a 60/s (fps30 já indicava isso); se não: é depois dele.
        new("grab120", "qsv-hwmap", EncodeSettings.Default with { GrabFps = 120 }),
    ];
}
