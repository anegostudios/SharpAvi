using SharpAvi.Output;
using SharpAvi.Utilities;

namespace SharpAvi.Codecs
{
    /// <summary>
    /// Contains extension methods for creating video streams.
    /// </summary>
    public static class SkiaSharpEncoderStreamFactory
    {
        /// <summary>
        /// Adds new video stream with <see cref="MJpegSkiaSharpVideoEncoder"/>.
        /// </summary>
        /// <param name="writer">Writer object to which new stream is added.</param>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <param name="quality">Requested quality of compression.</param>
        /// <param name="flip">Whether to vertically flip the frame before writing.</param>
        /// <seealso cref="AviWriter.AddEncodingVideoStream"/>
        /// <seealso cref="MJpegSkiaSharpVideoEncoder"/>
        public static IAviVideoStream AddMJpegSkiaSharpVideoStream(this AviWriter writer, int width, int height, int quality = 70, bool flip = false)
        {
            Argument.IsNotNull(writer, nameof(writer));
            Argument.IsPositive(width, nameof(width));
            Argument.IsPositive(height, nameof(height));
            Argument.IsInRange(quality, 1, 100, nameof(quality));

            var encoder = new MJpegSkiaSharpVideoEncoder(width, height, quality, flip);
            return writer.AddEncodingVideoStream(encoder, true, width, height);
        }

    }
}
