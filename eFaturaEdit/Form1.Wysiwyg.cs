using System;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using FontAwesome.Sharp;

namespace eFaturaEdit
{
    public partial class Form1
    {
        #region WYSIWYG Biçimlendirme Toolbar

        /// <summary>
        /// Ribbon'a WYSIWYG biçimlendirme araç çubuğunu ekler.
        /// Mevcut Bold/Italic/Underline/Align butonlarını aktifleştirir
        /// ve yeni biçimlendirme butonları ekler.
        /// </summary>
        private void InitWysiwygToolbar()
        {
            // Biçimlendirme sekmesini oluştur
            _formatRibbonPage = new RibbonPage { Text = "Biçimlendirme" };
            ribbonControl.Pages.Insert(1, _formatRibbonPage);

            var formatColor = Color.FromArgb(33, 33, 33);

            // ── Yazı Biçimi grubu ──
            var fontGroup = new RibbonPageGroup("Yazı Biçimi");

            // Mevcut butonlara handler bağla ve gruba ekle
            iBoldFontStyle.Glyph = FontAwesome.Sharp.IconChar.Bold.ToBitmap(formatColor, 16);
            iBoldFontStyle.ItemClick += WysiwygBold_ItemClick;
            fontGroup.ItemLinks.Add(iBoldFontStyle);

            iItalicFontStyle.Glyph = FontAwesome.Sharp.IconChar.Italic.ToBitmap(formatColor, 16);
            iItalicFontStyle.ItemClick += WysiwygItalic_ItemClick;
            fontGroup.ItemLinks.Add(iItalicFontStyle);

            iUnderlinedFontStyle.Glyph = FontAwesome.Sharp.IconChar.Underline.ToBitmap(formatColor, 16);
            iUnderlinedFontStyle.ItemClick += WysiwygUnderline_ItemClick;
            fontGroup.ItemLinks.Add(iUnderlinedFontStyle);

            // Üstü çizili
            var btnStrike = new BarButtonItem
            {
                Caption = "Üstü Çizili",
                Name = "iStrikethrough",
                Glyph = FontAwesome.Sharp.IconChar.Strikethrough.ToBitmap(formatColor, 16),
            };
            btnStrike.ItemClick += WysiwygStrikethrough_ItemClick;
            ribbonControl.Items.Add(btnStrike);
            fontGroup.ItemLinks.Add(btnStrike);

            // Üst simge
            var btnSuperscript = new BarButtonItem
            {
                Caption = "Üst Simge",
                Name = "iSuperscript",
                Glyph = FontAwesome.Sharp.IconChar.Superscript.ToBitmap(formatColor, 16),
            };
            btnSuperscript.ItemClick += WysiwygSuperscript_ItemClick;
            ribbonControl.Items.Add(btnSuperscript);
            fontGroup.ItemLinks.Add(btnSuperscript);

            // Alt simge
            var btnSubscript = new BarButtonItem
            {
                Caption = "Alt Simge",
                Name = "iSubscript",
                Glyph = FontAwesome.Sharp.IconChar.Subscript.ToBitmap(formatColor, 16),
            };
            btnSubscript.ItemClick += WysiwygSubscript_ItemClick;
            ribbonControl.Items.Add(btnSubscript);
            fontGroup.ItemLinks.Add(btnSubscript);

            _formatRibbonPage.Groups.Add(fontGroup);

            // ── Hizalama grubu ──
            var alignGroup = new RibbonPageGroup("Hizalama");

            iLeftTextAlign.Glyph = FontAwesome.Sharp.IconChar.AlignLeft.ToBitmap(formatColor, 16);
            iLeftTextAlign.ItemClick += WysiwygAlignLeft_ItemClick;
            alignGroup.ItemLinks.Add(iLeftTextAlign);

            iCenterTextAlign.Glyph = FontAwesome.Sharp.IconChar.AlignCenter.ToBitmap(formatColor, 16);
            iCenterTextAlign.ItemClick += WysiwygAlignCenter_ItemClick;
            alignGroup.ItemLinks.Add(iCenterTextAlign);

            iRightTextAlign.Glyph = FontAwesome.Sharp.IconChar.AlignRight.ToBitmap(formatColor, 16);
            iRightTextAlign.ItemClick += WysiwygAlignRight_ItemClick;
            alignGroup.ItemLinks.Add(iRightTextAlign);

            _formatRibbonPage.Groups.Add(alignGroup);

            // ── Renk & Boyut grubu ──
            var styleGroup = new RibbonPageGroup("Stil");

            // Yazı rengi
            var btnFontColor = new BarButtonItem
            {
                Caption = "Yazı Rengi",
                Name = "iFontColor",
                Glyph = FontAwesome.Sharp.IconChar.Palette.ToBitmap(Color.FromArgb(229, 57, 53), 16),
            };
            btnFontColor.ItemClick += WysiwygFontColor_ItemClick;
            ribbonControl.Items.Add(btnFontColor);
            styleGroup.ItemLinks.Add(btnFontColor);

            // Arka plan rengi
            var btnBgColor = new BarButtonItem
            {
                Caption = "Arka Plan Rengi",
                Name = "iBgColor",
                Glyph = FontAwesome.Sharp.IconChar.FillDrip.ToBitmap(Color.FromArgb(255, 193, 7), 16),
            };
            btnBgColor.ItemClick += WysiwygBgColor_ItemClick;
            ribbonControl.Items.Add(btnBgColor);
            styleGroup.ItemLinks.Add(btnBgColor);

            // Yazı boyutu — alt menü
            var subFontSize = new BarSubItem
            {
                Caption = "Yazı Boyutu",
                Name = "iFontSize",
                Glyph = FontAwesome.Sharp.IconChar.TextHeight.ToBitmap(formatColor, 16),
            };
            string[] fontSizes = { "8", "9", "10", "11", "12", "14", "16", "18", "20", "24", "28", "32", "36", "48" };
            foreach (string size in fontSizes)
            {
                var sizeBtn = new BarButtonItem { Caption = size + "pt", Tag = size, Name = "iFontSize_" + size };
                sizeBtn.ItemClick += WysiwygFontSize_ItemClick;
                subFontSize.AddItem(sizeBtn);
                ribbonControl.Items.Add(sizeBtn);
            }
            ribbonControl.Items.Add(subFontSize);
            styleGroup.ItemLinks.Add(subFontSize);

            _formatRibbonPage.Groups.Add(styleGroup);

            // ── Ekleme grubu ──
            var insertGroup = new RibbonPageGroup("Ekle");

            // Yatay çizgi
            var btnHr = new BarButtonItem
            {
                Caption = "Yatay Çizgi",
                Name = "iWysiwygHr",
                Glyph = FontAwesome.Sharp.IconChar.GripLines.ToBitmap(formatColor, 16),
            };
            btnHr.ItemClick += WysiwygHr_ItemClick;
            ribbonControl.Items.Add(btnHr);
            insertGroup.ItemLinks.Add(btnHr);

            // Sıralı liste
            var btnOl = new BarButtonItem
            {
                Caption = "Sıralı Liste",
                Name = "iOrderedList",
                Glyph = FontAwesome.Sharp.IconChar.ListOl.ToBitmap(formatColor, 16),
            };
            btnOl.ItemClick += WysiwygOrderedList_ItemClick;
            ribbonControl.Items.Add(btnOl);
            insertGroup.ItemLinks.Add(btnOl);

            // Sırasız liste
            var btnUl = new BarButtonItem
            {
                Caption = "Madde İşareti",
                Name = "iUnorderedList",
                Glyph = FontAwesome.Sharp.IconChar.ListUl.ToBitmap(formatColor, 16),
            };
            btnUl.ItemClick += WysiwygUnorderedList_ItemClick;
            ribbonControl.Items.Add(btnUl);
            insertGroup.ItemLinks.Add(btnUl);

            // Kenarlık / border
            var btnBorder = new BarButtonItem
            {
                Caption = "Kenarlık",
                Name = "iWysiwygBorder",
                Glyph = FontAwesome.Sharp.IconChar.BorderAll.ToBitmap(formatColor, 16),
            };
            btnBorder.ItemClick += WysiwygBorder_ItemClick;
            ribbonControl.Items.Add(btnBorder);
            insertGroup.ItemLinks.Add(btnBorder);

            _formatRibbonPage.Groups.Add(insertGroup);
        }

        // ── Event Handler'lar ──

        private void WysiwygBold_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "b");
        }

        private void WysiwygItalic_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "i");
        }

        private void WysiwygUnderline_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "u");
        }

        private void WysiwygStrikethrough_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "s");
        }

        private void WysiwygSuperscript_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "sup");
        }

        private void WysiwygSubscript_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapSelection(textEditorControlEx1, "sub");
        }

        private void WysiwygAlignLeft_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapWithAlignment(textEditorControlEx1, "left");
        }

        private void WysiwygAlignCenter_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapWithAlignment(textEditorControlEx1, "center");
        }

        private void WysiwygAlignRight_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapWithAlignment(textEditorControlEx1, "right");
        }

        private void WysiwygFontColor_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string hex = ColorTranslator.ToHtml(dlg.Color);
                    WysiwygHelper.WrapWithStyle(textEditorControlEx1, "color:" + hex);
                }
            }
        }

        private void WysiwygBgColor_ItemClick(object sender, ItemClickEventArgs e)
        {
            using (var dlg = new ColorDialog())
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string hex = ColorTranslator.ToHtml(dlg.Color);
                    WysiwygHelper.WrapWithStyle(textEditorControlEx1, "background-color:" + hex);
                }
            }
        }

        private void WysiwygFontSize_ItemClick(object sender, ItemClickEventArgs e)
        {
            string size = e.Item.Tag as string;
            if (string.IsNullOrEmpty(size)) return;
            WysiwygHelper.WrapWithStyle(textEditorControlEx1, "font-size:" + size + "pt");
        }

        private void WysiwygHr_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.InsertAtCursor(textEditorControlEx1, "<hr />");
        }

        private void WysiwygOrderedList_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.InsertAtCursor(textEditorControlEx1,
                "<ol>\n  <li></li>\n  <li></li>\n  <li></li>\n</ol>");
        }

        private void WysiwygUnorderedList_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.InsertAtCursor(textEditorControlEx1,
                "<ul>\n  <li></li>\n  <li></li>\n  <li></li>\n</ul>");
        }

        private void WysiwygBorder_ItemClick(object sender, ItemClickEventArgs e)
        {
            WysiwygHelper.WrapWithStyle(textEditorControlEx1, "border:1px solid #000;padding:4px");
        }

        #endregion
    }
}
