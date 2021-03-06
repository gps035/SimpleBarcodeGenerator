// <copyright file="BarcodeBuilder.cs" company="Dataphiles Ltd.">
//   Copyright (c) 2016 Dataphiles Ltd.
// </copyright>

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using JetBrains.Annotations;
using Zen.Barcode;

namespace SimpleBarcodeGenerator
{
	public class BarcodeBuilder
	{
		private readonly string _value;
		private string _caption;
		private string _fontFamily;
		private Size _size;
		private BarcodeSymbology _type;
		private bool _addCharacterSpacing;

		private BarcodeBuilder(string value)
		{
			_value = value;
		}

		[NotNull]
		public static BarcodeBuilder New([NotNull] string value)
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value), "A barcode value is required");
			}
			//Set defaults
			return new BarcodeBuilder(value)
				.Caption(value)
				.CaptionFontFamily("Courier New")
				.Type(BarcodeSymbology.Code128)
				.CharacterSpacing(true)
				.Size(200, 100);
		}

		[NotNull]
		public BarcodeBuilder Caption([CanBeNull] string caption)
		{
			_caption = caption;
			return this;
		}

		[NotNull]
		public BarcodeBuilder Size(Size size)
		{
			_size = size;
			return this;
		}

		[NotNull]
		public BarcodeBuilder Size(int width, int height) => Size(new Size(width, height));

		[NotNull]
		public BarcodeBuilder Type(BarcodeSymbology type)
		{
			_type = type;
			return this;
		}

		[NotNull]
		public BarcodeBuilder CharacterSpacing(bool spacing)
		{
			_addCharacterSpacing = spacing;
			return this;
		}

		[NotNull]
		public BarcodeBuilder CaptionFontFamily([NotNull] string family)
		{
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (family == null)
			{
				throw new ArgumentNullException(nameof(family), "A caption font family is required");
			}
			_fontFamily = family;
			return this;
		}

		[NotNull]
		public string GenerateImageString([NotNull] ImageFormat imageFormat)
			=> Convert.ToBase64String(GenerateByteArray(imageFormat));

		[NotNull]
		public byte[] GenerateByteArray([NotNull] ImageFormat imageFormat)
		{
			using (var ms = new MemoryStream())
			{
				using (var image = GenerateImage())
				{
					image.Save(ms, imageFormat);
				}
				return ms.ToArray();
			}
		}

		[NotNull]
		public Image GenerateImage()
		{
			//For some reason, you cant get it to generate at the right size, so lets make it too big, and scale down
			var max = Math.Max(_size.Width, _size.Height);
			var size = new Size(max, max);
			var factory = BarcodeDrawFactory.GetSymbology(_type);
			using (var barcode = factory.Draw(_value, factory.GetPrintMetrics(size, size, _value.Length)))
			{
				if (_caption == null)
				{
					return new Bitmap(barcode, new Size(_size.Width, _size.Height));
				}
				var textHeight = _size.Height / 5;
				var bitmap = new Bitmap(_size.Width, _size.Height);
				using (var g = Graphics.FromImage(bitmap))
				{
					g.DrawImage(barcode, 0, 0, _size.Width, _size.Height - textHeight);
					//Append another image for the text underneath
					using (var text = TextToImage(_caption, _size.Width, textHeight, _fontFamily, _addCharacterSpacing))
					{
						g.DrawImage(text, 0, _size.Height - textHeight);
					}
				}
				return bitmap;
			}
		}

		[NotNull]
		private static Image TextToImage(string text, int width, int height, string fontFamily, bool addCharacterSpacing)
		{
			var bmp = new Bitmap(width, height);
			using (var testFont = new Font(fontFamily, 50))
			{
				using (var g = Graphics.FromImage(bmp))
				{
					var stringSize = g.MeasureString(text, testFont);
					var heightRatio = bmp.Height / stringSize.Height;
					var widthRatio = bmp.Width / stringSize.Width;
					float fontSize;
					if (heightRatio < widthRatio)
					{
						fontSize = testFont.Size * heightRatio;
					}
					else
					{
						fontSize = testFont.Size * widthRatio;
					}
					using (var font = new Font(testFont.FontFamily, fontSize))
					{
						var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
						var format = StringFormat.GenericDefault;
						format.Alignment = StringAlignment.Center;
						format.LineAlignment = StringAlignment.Center;
						format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
						if (addCharacterSpacing)
						{
							var noOfSpaces = 0;
							while (g.MeasureString(PadCharacters(text, noOfSpaces + 1), font, new SizeF(width * 2, height), format)
							        .Width < width)
							{
								noOfSpaces += 1;
							}
							text = PadCharacters(text, noOfSpaces);
						}
						using (var whiteBrush = new SolidBrush(Color.White))
						{
							using (var blackBrush = new SolidBrush(Color.Black))
							{
								g.FillRectangle(whiteBrush, rect);
								g.DrawString(text, font, blackBrush, rect, format);
								g.Flush();
							}
						}
					}
				}
			}
			return bmp;
		}

		[CanBeNull]
		private static string PadCharacters([CanBeNull] string input, int spaceCount)
		{
			if (string.IsNullOrWhiteSpace(input) || spaceCount <= 0)
			{
				return input;
			}
			var output = "";
			for (var i = 0; i < spaceCount; i++)
			{
				output += " ";
			}
			foreach (var character in input)
			{
				output += character;
				for (var i = 0; i < spaceCount; i++)
				{
					output += " ";
				}
			}
			return output;
		}
	}
}
