using LunarLabs.Fonts;
using System.Diagnostics;
using System.Text;

namespace DaisyTTF
{
	class Program
	{
		private struct Arguments
		{
			public string FontFilepath;
			public uint Size;
			public uint BitsPerPixel;
			public bool AddWidth;
			public bool DebugView;
		}

		const char FIRST_CHARACTER = ' ';
		const char LAST_CHARACTER = '~';
		const uint DATA_BIT_COUNT = sizeof(ulong) * 8;

		private static void Main(string[] args)
		{
			args = new string[] { "Z:/impact.ttf", "64", "2" };

			Arguments arguments = GetArguments(args);
			if (!File.Exists(arguments.FontFilepath))
				throw new FileNotFoundException(arguments.FontFilepath);

			arguments.AddWidth = true;
			arguments.DebugView = true;

			Font font = new Font(File.ReadAllBytes(arguments.FontFilepath));

			string variableName = Path.GetFileNameWithoutExtension(arguments.FontFilepath);
			variableName = variableName.Replace(" ", string.Empty);
			variableName = variableName.Replace('-', '_');
			variableName = $"Font_{variableName}_{arguments.Size}_{arguments.BitsPerPixel}BPP";

			string output = BuildBitmapData(variableName, font, arguments);

			Console.Write(output);
		}

		private static string BuildBitmapData(string variableName, Font font, Arguments arguments)
		{
			string dataVariableName = $"{variableName}_DATA";

			StringBuilder output = new StringBuilder();
			output.AppendLine($"static const uint{DATA_BIT_COUNT} {dataVariableName}[] = {{");

			uint maxWidth = 0;
			for (char c = FIRST_CHARACTER; c <= LAST_CHARACTER; ++c)
			{
				uint width;
				AppendGlyphData(output, font, c, arguments, out width);

				if (width > maxWidth)
					maxWidth = width;
			}

			output.AppendLine("};");

			output.AppendLine("");

			output.AppendLine($"static const Font {variableName} = {{{maxWidth}, {arguments.Size}, {dataVariableName}, 1, {arguments.BitsPerPixel}, '{FIRST_CHARACTER}', '{LAST_CHARACTER}', {arguments.AddWidth.ToString().ToLower()}}};");

			return output.ToString();
		}

		private static void AppendGlyphData(StringBuilder output, Font font, char c, Arguments arguments, out uint width)
		{
			FontGlyph glyph = font.RenderGlyph(c, font.ScaleInPixels(arguments.Size));
			GlyphBitmap image = glyph.Image;

			width = (uint)glyph.xAdvance;

			output.Append("\t");

			if (arguments.AddWidth)
				output.Append($"0x{width.ToString("X2")}, ");

			uint pixelCountPerElement = Math.Min(arguments.Size, DATA_BIT_COUNT / arguments.BitsPerPixel);
			uint elementCountPerRow = Math.Max(1, arguments.Size / (DATA_BIT_COUNT / arguments.BitsPerPixel));

			uint totalWrittenElementCount = 0;

			for (uint y = 0; y < arguments.Size; ++y)
			{
				int indexY = (int)y - (glyph.yOfs + (int)arguments.Size);

				uint writtenBitCount = 0;
				uint writtenElementCount = 0;
				ulong data = 0;
				void flush()
				{
					if (arguments.DebugView)
						output.Append($"0b{data.ToString($"B64")}, ");
					else
						output.Append($"0x{data.ToString($"X16")}, ");

					writtenBitCount = 0;
					data = 0;

					++writtenElementCount;
					++totalWrittenElementCount;
				}

				for (uint x = 0; x < arguments.Size; ++x)
				{
					int indexX = (int)x - glyph.xOfs;

					if (0 <= indexX && indexX < image.Width &&
						0 <= indexY && indexY < image.Height)
					{
						byte pixelValue = image.Pixels[indexX + (indexY * image.Width)];

						byte b = PixelValueToBit(pixelValue, (int)arguments.BitsPerPixel);

						data |= ((ulong)b << (int)(x * arguments.BitsPerPixel));
					}

					writtenBitCount += arguments.BitsPerPixel;

					if ((x + 1) % pixelCountPerElement == 0)
						flush();
				}

				for (; writtenElementCount < elementCountPerRow; ++writtenElementCount)
					flush();

				if (arguments.DebugView)
					output.AppendLine();
			}

			output.AppendLine($"// [{c}]");

			Debug.Assert(totalWrittenElementCount == elementCountPerRow * arguments.Size);
		}

		private static byte[,] GetGlyphPixelData(Font font, char c, Arguments arguments)
		{
			FontGlyph glyph = font.RenderGlyph(c, font.ScaleInPixels(arguments.Size));
			GlyphBitmap image = glyph.Image;

			byte[,] canvas = new byte[arguments.Size, arguments.Size];

			int startX = glyph.xOfs;
			int startY = (int)arguments.Size + glyph.yOfs;

			for (int iy = 0; iy < image.Height; iy++)
			{
				for (int ix = 0; ix < image.Width; ix++)
				{
					int canvasX = startX + ix;
					int canvasY = startY + iy;

					if (canvasX >= 0 && canvasX < arguments.Size && canvasY >= 0 && canvasY < arguments.Size)
					{
						canvas[canvasY, canvasX] = image.Pixels[ix + (iy * image.Width)];
					}
				}
			}

			return canvas;
		}

		private static Arguments GetArguments(string[] args)
		{
			if (args.Length != 3)
				throw new ArgumentException();

			Arguments arguments = new Arguments();

			arguments.FontFilepath = args[0];

			arguments.Size = Convert.ToUInt32(args[1]);
			arguments.Size = Math.Min(64, Math.Max(10, arguments.Size));

			arguments.BitsPerPixel = Convert.ToUInt32(args[2]);
			arguments.BitsPerPixel = Math.Min(4, Math.Max(1, arguments.BitsPerPixel));

			return arguments;
		}

		private static byte PixelValueToBit(byte pixelValue, int bitsPerPixel)
		{
			if (bitsPerPixel == 1)
			{
				return (byte)(pixelValue < 128 ? 0 : 1);
			}

			if (bitsPerPixel == 2)
			{
				byte[] Compositions = new byte[] { 0b00, 0b10, 0b11, 0b01 };

				int index = (int)(pixelValue / 85.0);

				return Compositions[index];
			}

			return 0;
		}
	}
}