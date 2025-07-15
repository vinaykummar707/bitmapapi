using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace BitmapAsciiApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BitmapController : ControllerBase
    {
        [HttpGet]
        public IActionResult GenerateAscii(
            [FromQuery] string text = "",
            [FromQuery] int height = 24,
            [FromQuery] string weight = "Regular",
            [FromQuery] string family = "Arial",
            [FromQuery] int pleft = 1,
            [FromQuery] int pright = 3,
            [FromQuery] bool padding = false)
        {
            // Create font
            var font = new Font(family, height, weight == "Regular" ? FontStyle.Regular : FontStyle.Bold, GraphicsUnit.Pixel);

            // Measure text size
            using var tempBmp = new Bitmap(1, 1);
            using var gTemp = Graphics.FromImage(tempBmp);
            var size = gTemp.MeasureString(text, font);
            int width = (int)Math.Ceiling(size.Width);

            // Render to bitmap
            using var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Black);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            g.DrawString(text, font, Brushes.White, new PointF(0, 0));

            // Crop content
            var cropRect = GetContentBounds(bmp);
            if (cropRect == Rectangle.Empty)
                return BadRequest("No content found.");

            using var cropped = bmp.Clone(cropRect, bmp.PixelFormat);

            Bitmap finalBmp;

            if (padding)
            {
                // Add 1 column at start and 2 at end
                int leftPadding = pleft;
                int rightPadding = pright;
                int paddedWidth = cropped.Width + leftPadding + rightPadding;
                int paddedHeight = cropped.Height;
                finalBmp = new Bitmap(paddedWidth, paddedHeight, cropped.PixelFormat);

                using (var gPad = Graphics.FromImage(finalBmp))
                {
                    gPad.Clear(Color.Black);
                    // Draw original cropped bitmap starting at x=1
                    gPad.DrawImageUnscaled(cropped, leftPadding, 0);
                }
            }
            else
            {
                // No padding, use original cropped
                finalBmp = new Bitmap(cropped);
            }

            // Convert to ASCII
            var sb = new StringBuilder();
            for (int y = 0; y < finalBmp.Height; y++)
            {
                for (int x = 0; x < finalBmp.Width; x++)
                {
                    Color pixel = finalBmp.GetPixel(x, y);
                    int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                    sb.Append(brightness > 128 ? "#" : ".");
                }
                   if (y < cropped.Height - 1) // ⬅️ Don't add \n on the last line
                    sb.Append('\n');
            }
            

            return Ok(sb.ToString());
        }

        private Rectangle GetContentBounds(Bitmap bmp)
        {
            int minX = bmp.Width, maxX = 0;
            int minY = bmp.Height, maxY = 0;
            bool found = false;

            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color pixel = bmp.GetPixel(x, y);
                    int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                    if (brightness > 20)
                    {
                        found = true;
                        minX = Math.Min(minX, x);
                        maxX = Math.Max(maxX, x);
                        minY = Math.Min(minY, y);
                        maxY = Math.Max(maxY, y);
                    }
                }
            }

            return found ? Rectangle.FromLTRB(minX, minY, maxX + 1, maxY + 1) : Rectangle.Empty;
        }
    }
}
