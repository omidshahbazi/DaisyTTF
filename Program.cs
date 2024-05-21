using LunarLabs.Fonts;
using System.Text;
using System.Xml.Linq;

namespace DaisyTTF
{
	class Program
	{
		const char FIRST_CHARACTER = ' ';
		const char LAST_CHARACTER = '~';

		static void Main(string[] args)
		{
			args = new string[] { "E:/DUBAI-MEDIUM.ttf", "32" };

			if (args.Length != 2)
				throw new ArgumentException();

			string ttfFilepath = args[0];
			if (!File.Exists(ttfFilepath))
				throw new FileNotFoundException(ttfFilepath);

			int size = Convert.ToInt32(args[1]);
			size = Math.Min(32, Math.Max(8, size));

			Font font = new Font(File.ReadAllBytes(ttfFilepath));

			float correctPixelScale = size * 1.6F;
			while (true)
			{
				FontGlyph glyph = font.RenderGlyph('Q', font.ScaleInPixels(correctPixelScale));
				if (glyph.Image.Height > size)
				{
					correctPixelScale -= 0.01F;
					continue;
				}

				break;
			}

			string variableName = Path.GetFileNameWithoutExtension(ttfFilepath);
			variableName = variableName.Replace(" ", string.Empty);
			variableName = variableName.Replace('-', '_');
			variableName += $"_{size}";

			string dataVariableName = $"{variableName}_DATA";

			StringBuilder output = new StringBuilder();
			output.AppendLine($"static const uint32 {dataVariableName}[] = {{");

			float scale = font.ScaleInPixels(correctPixelScale);
			int maxWidth = 0;
			for (char c = FIRST_CHARACTER; c <= LAST_CHARACTER; ++c)
			{
				FontGlyph glyph = font.RenderGlyph(c, scale);
				GlyphBitmap image = glyph.Image;

				output.Append("\t");

				for (int y = 0; y < size; ++y)
				{
					int indexY = y - (glyph.yOfs + size);

					int rowData = 0;

					if (indexY >= 0)
						for (int x = 0; x < size; ++x)
						{
							int indexX = x - glyph.xOfs;

							if (indexX < 0)
								continue;
							if (image.Height <= indexY)
								break;
							if (image.Width <= indexX)
								break;

							if (image.Width > maxWidth)
								maxWidth = image.Width;

							if (image.Pixels[indexX + (indexY * image.Width)] < 250)
								continue;

							rowData |= (1 << x);
						}

					output.Append($"0x{rowData.ToString("X8")}, ");
				}

				output.AppendLine($"// [{c}]");
			}

			output.AppendLine("};");

			output.AppendLine("");

			output.AppendLine($"static const Font Font_{variableName} = {{{maxWidth}, {size}, {dataVariableName}}};");

			Console.Write(output.ToString());
		}
	}
}