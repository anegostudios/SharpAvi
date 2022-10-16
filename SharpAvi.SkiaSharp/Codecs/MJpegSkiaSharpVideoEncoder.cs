using System;
using System.IO;
using SharpAvi.Utilities;
using SkiaSharp;

namespace SharpAvi.Codecs
{
    /// <summary>
    /// Encodes frames in Motion JPEG format.
    /// </summary>
    /// <remarks>
    /// The implementation relies on <see cref="SKImage"/> from the <c>SkiaSharp.SKImage</c> package.
    /// </remarks>
    public sealed class MJpegSkiaSharpVideoEncoder : IVideoEncoder
    {
        private readonly int width;
        private readonly int height;
        private readonly int quality;
        private byte[] sourceBuffer;
#if NET5_0_OR_GREATER
        private readonly MemoryStream buffer;
#endif

        /// <summary>
        /// Creates a new instance of <see cref="MJpegSkiaSharpVideoEncoder"/>.
        /// </summary>
        /// <param name="width">Frame width.</param>
        /// <param name="height">Frame height.</param>
        /// <param name="quality">
        /// Compression quality in the range [1..100].
        /// Less values mean less size and lower image quality.
        /// </param>
        /// <param name="flip">Whether to vertically flip the frame before writing.</param>
        public MJpegSkiaSharpVideoEncoder(int width, int height, int quality, bool flip)
        {
            Argument.IsPositive(width, nameof(width));
            Argument.IsPositive(height, nameof(height));
            Argument.IsInRange(quality, 1, 100, nameof(quality));

            this.width = width;
            this.height = height;
            this.quality = quality;
            sourceBuffer = new byte[width * height * 4];
            FlipVertical = flip;
#if NET5_0_OR_GREATER
            buffer = new MemoryStream(MaxEncodedSize);
#endif
        }

        /// <summary>Video codec.</summary>
        public FourCC Codec => CodecIds.MotionJpeg;

        /// <summary>
        /// Number of bits per pixel in encoded image.
        /// </summary>
        public BitsPerPixel BitsPerPixel => BitsPerPixel.Bpp24;

        /// <summary>
        /// Maximum size of encoded frmae.
        /// </summary>
        public int MaxEncodedSize => Math.Max(width * height * 3, 1024);

        /// <summary>
        /// Whether to vertically flip the frame before writing
        /// </summary>
        public bool FlipVertical
        {
            get; set; 
        }

        /// <summary>
        /// Encodes a frame.
        /// </summary>
        public int EncodeFrame(byte[] source, int srcOffset, byte[] destination, int destOffset, out bool isKeyFrame)
        {
            Argument.IsNotNull(source, nameof(source));
            Argument.IsNotNegative(srcOffset, nameof(srcOffset));
            Argument.ConditionIsMet(srcOffset + 4 * width * height <= source.Length,
                "Source end offset exceeds the source length.");
            Argument.IsNotNull(destination, nameof(destination));
            Argument.IsNotNegative(destOffset, nameof(destOffset));

            int length;
            using (var stream = new MemoryStream(destination))
            {
                stream.Position = destOffset;
                if (FlipVertical)
                {
                    if (sourceBuffer == null)
                    {
                        sourceBuffer = new byte[width * height * 4];
                    }
#if NET5_0_OR_GREATER
                    BitmapUtils.FlipVertical(source, sourceBuffer, height, width * 4);
#else
                    BitmapUtils.FlipVertical(source, srcOffset, sourceBuffer, 0, height, width * 4);
#endif
                }
                else
                {
                    sourceBuffer = source;
                }
                length = LoadAndEncodeImage(sourceBuffer.AsSpan(srcOffset), stream);
            }

            isKeyFrame = true;
            return length;
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Encodes a frame.
        /// </summary>
        public int EncodeFrame(ReadOnlySpan<byte> source, Span<byte> destination, out bool isKeyFrame)
        {
            Argument.ConditionIsMet(4 * width * height <= source.Length,
                "Source end offset exceeds the source length.");

            buffer.SetLength(0);
            if (FlipVertical)
            {
                if (sourceBuffer == null)
                {
                    sourceBuffer = new byte[width * height * 4];
                }
                BitmapUtils.FlipVertical(source, sourceBuffer, height, width * 4);
            }
            else
            {
                sourceBuffer = source.ToArray();
            }
            var length = LoadAndEncodeImage(source, buffer);
            buffer.GetBuffer().AsSpan(0, length).CopyTo(destination);

            isKeyFrame = true;
            return length;
        }
#endif        
        private int LoadAndEncodeImage(ReadOnlySpan<byte> source, Stream destination)
        {
            var startPosition = (int)destination.Position;
            using (var image = SKImage.FromPixelCopy(new SKImageInfo(width, height), source))
            {
                var encoded = image.Encode(SKEncodedImageFormat.Jpeg, quality);
                encoded.SaveTo(destination);
            }
            destination.Flush();
            return (int)(destination.Position - startPosition);
        }
    }
}
