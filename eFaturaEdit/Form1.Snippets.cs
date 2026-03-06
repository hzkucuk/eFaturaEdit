using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraBars.Ribbon;
using System.IO;
using FontAwesome.Sharp;

namespace eFaturaEdit
{
    public partial class Form1
    {
        #region Öğe Ekleme Toolbar — Faz 1 (Editör) + Faz 2 (Önizleme)

        /// <summary>
        /// Ribbon'a snippet'leri kategorilerine göre ayrı sekmelere ekler.
        /// "Öğeler" sekmesi: HTML Öğeleri, XSLT Komutları, Sayfa Düzeni.
        /// "UBL-TR" sekmesi: e-Fatura, e-Arşiv, e-İrsaliye.
        /// SubCategory varsa alt menü (BarSubItem) oluşturur.
        /// </summary>
        private void InitSnippetToolbar()
        {
            // Yeni sekmeleri oluştur
            _snippetRibbonPage = new RibbonPage { Text = "Öğeler" };
            _ublRibbonPage = new RibbonPage { Text = "UBL-TR" };
            ribbonControl.Pages.Add(_snippetRibbonPage);
            ribbonControl.Pages.Add(_ublRibbonPage);

            var grouped = XsltSnippets.Elements.Values
                .GroupBy(s => s.Category)
                .OrderBy(g => GetCategoryOrder(g.Key));

            foreach (var group in grouped)
            {
                var pageGroup = new RibbonPageGroup(group.Key);

                // SubCategory'ye göre alt gruplama
                var subGrouped = group
                    .GroupBy(s => s.SubCategory ?? string.Empty)
                    .OrderBy(sg => sg.Key);

                foreach (var subGroup in subGrouped)
                {
                    var catColor = GetCategoryColor(group.Key);

                    if (string.IsNullOrEmpty(subGroup.Key))
                    {
                        // SubCategory yok — doğrudan buton ekle
                        foreach (var snippet in subGroup)
                        {
                            var btn = new BarButtonItem
                            {
                                Caption = snippet.DisplayName,
                                Tag = snippet.Key,
                                Name = "btnSnippet_" + snippet.Key,
                                AllowAllUp = true,
                                Glyph = CreateSnippetIcon(snippet.Key, catColor),
                            };
                            btn.SuperTip = CreateSnippetTooltip(snippet);
                            btn.ItemClick += SnippetButton_ItemClick;

                            ribbonControl.Items.Add(btn);
                            pageGroup.ItemLinks.Add(btn);
                        }
                    }
                    else
                    {
                        // SubCategory var — alt menü oluştur
                        var subMenu = new BarSubItem
                        {
                            Caption = subGroup.Key,
                            Name = "subSnippet_" + group.Key.Replace(" ", "") + "_" + subGroup.Key.Replace(" ", ""),
                            Glyph = CreateCategoryIcon(group.Key, catColor),
                        };

                        foreach (var snippet in subGroup)
                        {
                            var btn = new BarButtonItem
                            {
                                Caption = snippet.DisplayName,
                                Tag = snippet.Key,
                                Name = "btnSnippet_" + snippet.Key,
                                AllowAllUp = true,
                                Glyph = CreateSnippetIcon(snippet.Key, catColor),
                            };
                            btn.SuperTip = CreateSnippetTooltip(snippet);
                            btn.ItemClick += SnippetButton_ItemClick;

                            subMenu.AddItem(btn);
                            ribbonControl.Items.Add(btn);
                        }

                        ribbonControl.Items.Add(subMenu);
                        pageGroup.ItemLinks.Add(subMenu);
                    }
                }

                // Kategoriyi doğru sekmeye yönlendir
                GetRibbonPageForCategory(group.Key).Groups.Add(pageGroup);
            }

            // Snippet sürükleme için Ribbon MouseDown hook
            ribbonControl.MouseDown += RibbonControl_SnippetMouseDown;

            // Editör sağ tık menüsünü oluştur
            InitEditorContextMenu();
        }

        /// <summary>
        /// Snippet kategorisini doğru Ribbon sekmesine yönlendirir.
        /// </summary>
        private RibbonPage GetRibbonPageForCategory(string category)
        {
            switch (category)
            {
                case "UBL-TR e-Fatura":
                case "UBL-TR e-Arşiv":
                case "UBL-TR e-İrsaliye":
                    return _ublRibbonPage;
                default:
                    return _snippetRibbonPage;
            }
        }

        /// <summary>
        /// Kategori sıralama önceliği (Ribbon'da soldan sağa).
        /// </summary>
        private static int GetCategoryOrder(string category)
        {
            switch (category)
            {
                case "HTML Öğeleri": return 0;
                case "XSLT Komutları": return 1;
                case "Sayfa Düzeni": return 2;
                case "UBL-TR e-Fatura": return 3;
                case "UBL-TR e-Arşiv": return 4;
                default: return 99;
            }
        }

        #endregion

        #region Örnek XML Senaryo Toolbar

        /// <summary>
        /// Ribbon'a "Örnek Faturalar" grubu ve kategorilere göre alt menü butonları ekler.
        /// UBL-TR 1.2.1 resmi örnek XML dosyaları kullanıcıya sunulur.
        /// </summary>
        private void InitSampleXmlToolbar()
        {
            if (!UblTrSamples.SamplesDirectoryExists())
                return;

            var samplePageGroup = new RibbonPageGroup("Örnek Faturalar");

            foreach (var group in UblTrSamples.Groups)
            {
                var subMenu = new BarSubItem
                {
                    Caption = group.CategoryName,
                    Name = "subSample_" + group.CategoryName.Replace(" ", ""),
                };

                foreach (var entry in group.Entries)
                {
                    var item = new BarButtonItem
                    {
                        Caption = entry.DisplayName,
                        Tag = entry.FileName,
                        Name = "btnSample_" + Path.GetFileNameWithoutExtension(entry.FileName),
                    };
                    item.ItemClick += SampleXml_ItemClick;
                    subMenu.AddItem(item);
                    ribbonControl.Items.Add(item);
                }

                ribbonControl.Items.Add(subMenu);
                samplePageGroup.ItemLinks.Add(subMenu);
            }

            homeRibbonPage.Groups.Add(samplePageGroup);
        }

        /// <summary>
        /// Örnek XML seçildiğinde: XML'i editöre yükler, XSLT açıksa dönüşüm yapar ve önizler.
        /// </summary>
        private void SampleXml_ItemClick(object sender, ItemClickEventArgs e)
        {
            string fileName = e.Item.Tag as string;
            if (string.IsNullOrEmpty(fileName))
                return;

            string fullPath = UblTrSamples.GetFullPath(fileName);
            if (!File.Exists(fullPath))
            {
                MessageBox.Show(
                    $"Örnek XML dosyası bulunamadı:\n{fullPath}",
                    "Dosya Bulunamadı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // XML editörüne yükle
                textEditorControlEx2.LoadFile(fullPath, true, true);
                textEditorControlEx2.Tag = fullPath;
                xtraTabControl2.TabPages[1].Text = fileName;

                // XSLT açıksa dönüşüm yap ve önizle
                if (textEditorControlEx1.Tag != null)
                {
                    XsltTransformHelper.TransformXslFile(textEditorControlEx1.Tag.ToString(), fullPath, ResultHtmlPath);

                    CreateBrowserHost(ResultHtmlPath);
                    xtraTabControl2.TabPages[0].Text = fileName;
                }

                UpdateAndCheckFoldings();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Örnek XML yüklenirken hata oluştu:\n{ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Editör Sağ Tık Menüsü

        /// <summary>
        /// XSLT editörüne sağ tık context menüsü ekler.
        /// Snippet'ler Category → SubCategory → Snippet şeklinde ağaç yapısında gösterilir.
        /// </summary>
        private void InitEditorContextMenu()
        {
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.ShowImageMargin = true;
            ctxMenu.RenderMode = ToolStripRenderMode.Professional;

            // Snippet'leri kategoriye göre grupla
            var grouped = XsltSnippets.Elements.Values
                .GroupBy(s => s.Category)
                .OrderBy(g => GetCategoryOrder(g.Key));

            foreach (var group in grouped)
            {
                var catColor = GetCategoryColor(group.Key);
                var catItem = new ToolStripMenuItem(group.Key);
                catItem.Font = new Font(catItem.Font, FontStyle.Bold);
                catItem.Image = CreateCategoryIcon(group.Key, catColor);

                // SubCategory'ye göre alt gruplama
                var subGrouped = group
                    .GroupBy(s => s.SubCategory ?? string.Empty)
                    .OrderBy(sg => sg.Key);

                foreach (var subGroup in subGrouped)
                {
                    if (string.IsNullOrEmpty(subGroup.Key))
                    {
                        // SubCategory yok — doğrudan snippet ekle
                        foreach (var snippet in subGroup)
                        {
                            var item = CreateContextMenuItem(snippet, catColor);
                            catItem.DropDownItems.Add(item);
                        }
                    }
                    else
                    {
                        // SubCategory var — ara menü oluştur
                        var subCatItem = new ToolStripMenuItem(subGroup.Key);
                        subCatItem.Image = CreateCategoryIcon(group.Key, catColor);

                        foreach (var snippet in subGroup)
                        {
                            var item = CreateContextMenuItem(snippet, catColor);
                            subCatItem.DropDownItems.Add(item);
                        }

                        catItem.DropDownItems.Add(subCatItem);
                    }
                }

                ctxMenu.Items.Add(catItem);
            }

            // Editör TextArea'ya bağla
            textEditorControlEx1.ActiveTextAreaControl.TextArea.ContextMenuStrip = ctxMenu;
        }

        /// <summary>
        /// Sağ tık menüsü için snippet menü öğesi oluşturur.
        /// Renkli ikon + metin; ToolTipText tam açıklama metnini içerir (kısaltılmaz).
        /// </summary>
        private ToolStripMenuItem CreateContextMenuItem(SnippetInfo snippet, Color catColor)
        {
            var item = new ToolStripMenuItem(snippet.DisplayName);
            item.Image = CreateSnippetIcon(snippet.Key, catColor);
            item.ToolTipText = snippet.Description;
            item.Tag = snippet.Key;
            item.Click += ContextMenuSnippet_Click;
            return item;
        }

        /// <summary>
        /// Sağ tık menüsünden snippet seçildiğinde editöre ekler.
        /// </summary>
        private void ContextMenuSnippet_Click(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null) return;

            string key = menuItem.Tag as string;
            if (key == null || !XsltSnippets.Elements.ContainsKey(key))
                return;

            InsertSnippetAtCursor(XsltSnippets.Elements[key].XsltCode);
        }

        #endregion

        #region Snippet İkon Oluşturma (FontAwesome.Sharp)

        /// <summary>
        /// Her snippet anahtarına karşılık gelen FontAwesome ikonu.
        /// </summary>
        private static readonly Dictionary<string, FontAwesome.Sharp.IconChar> SnippetIconMap =
            new Dictionary<string, FontAwesome.Sharp.IconChar>
        {
            // ── HTML Öğeleri ──
            ["IMAGE"]   = FontAwesome.Sharp.IconChar.Image,
            ["TABLE"]   = FontAwesome.Sharp.IconChar.Table,
            ["TEXT"]    = FontAwesome.Sharp.IconChar.Paragraph,
            ["LINK"]    = FontAwesome.Sharp.IconChar.Link,
            ["HR"]      = FontAwesome.Sharp.IconChar.GripLines,
            ["DIV"]     = FontAwesome.Sharp.IconChar.BorderAll,
            ["BOLD"]    = FontAwesome.Sharp.IconChar.Bold,
            ["SPAN"]    = FontAwesome.Sharp.IconChar.Tag,

            // ── XSLT Komutları ──
            ["VALUEOF"]  = FontAwesome.Sharp.IconChar.Code,
            ["FOREACH"]  = FontAwesome.Sharp.IconChar.Redo,
            ["IF"]       = FontAwesome.Sharp.IconChar.QuestionCircle,

            // ── Sayfa Düzeni ──
            ["BARCODE"]   = FontAwesome.Sharp.IconChar.Barcode,
            ["QR"]        = FontAwesome.Sharp.IconChar.Qrcode,
            ["PAGEBREAK"] = FontAwesome.Sharp.IconChar.FileExport,
            ["HEADER"]    = FontAwesome.Sharp.IconChar.ArrowUp,
            ["FOOTER"]    = FontAwesome.Sharp.IconChar.ArrowDown,

            // ── UBL-TR e-Fatura — Başlık ──
            ["UBL_INVOICEHEADER"] = FontAwesome.Sharp.IconChar.FileInvoice,
            ["UBL_UUID"]          = FontAwesome.Sharp.IconChar.Key,
            ["UBL_ISSUETIME"]     = FontAwesome.Sharp.IconChar.Clock,
            ["UBL_NOTES"]         = FontAwesome.Sharp.IconChar.StickyNote,
            ["UBL_LINECOUNT"]     = FontAwesome.Sharp.IconChar.ListOl,
            ["UBL_COPYINDICATOR"] = FontAwesome.Sharp.IconChar.Copy,
            ["UBL_PROFILEID"]     = FontAwesome.Sharp.IconChar.Random,
            ["UBL_TYPECODE"]      = FontAwesome.Sharp.IconChar.Tags,
            ["UBL_CURRENCYID"]    = FontAwesome.Sharp.IconChar.MoneyBillAlt,

            // ── UBL-TR e-Fatura — Taraflar ──
            ["UBL_SUPPLIER"]          = FontAwesome.Sharp.IconChar.Building,
            ["UBL_CUSTOMER"]          = FontAwesome.Sharp.IconChar.User,
            ["UBL_SUPPLIER_CONTACT"]  = FontAwesome.Sharp.IconChar.Phone,
            ["UBL_CUSTOMER_CONTACT"]  = FontAwesome.Sharp.IconChar.MobileAlt,
            ["UBL_SUPPLIER_ADDRESS"]  = FontAwesome.Sharp.IconChar.MapMarkerAlt,
            ["UBL_CUSTOMER_ADDRESS"]  = FontAwesome.Sharp.IconChar.MapPin,
            ["UBL_PERSON"]            = FontAwesome.Sharp.IconChar.IdCard,
            ["UBL_PARTYIDS"]          = FontAwesome.Sharp.IconChar.IdBadge,
            ["UBL_WEBSITEURI"]        = FontAwesome.Sharp.IconChar.Globe,
            ["UBL_IDENTITYDOC"]       = FontAwesome.Sharp.IconChar.AddressCard,
            ["UBL_BUYERCUSTOMER"]     = FontAwesome.Sharp.IconChar.ShoppingCart,
            ["UBL_TAXREPRESENTATIVE"] = FontAwesome.Sharp.IconChar.Landmark,

            // ── UBL-TR e-Fatura — Kalemler ──
            ["UBL_INVOICELINES"]    = FontAwesome.Sharp.IconChar.ClipboardList,
            ["UBL_LINE_ALLOWANCE"]  = FontAwesome.Sharp.IconChar.Percent,
            ["UBL_LINE_WITHHOLDING"] = FontAwesome.Sharp.IconChar.Lock,

            // ── UBL-TR e-Fatura — Vergi ──
            ["UBL_TAXTOTAL"]      = FontAwesome.Sharp.IconChar.Coins,
            ["UBL_WITHHOLDING"]   = FontAwesome.Sharp.IconChar.BalanceScale,
            ["UBL_TAXEXEMPTION"]  = FontAwesome.Sharp.IconChar.Ban,
            ["UBL_TAXTYPEFILTER"] = FontAwesome.Sharp.IconChar.Filter,

            // ── UBL-TR e-Fatura — Toplamlar ──
            ["UBL_TOTALS"]          = FontAwesome.Sharp.IconChar.Calculator,
            ["UBL_CHARGETOTAL"]     = FontAwesome.Sharp.IconChar.PlusCircle,
            ["UBL_ALLOWANCECHARGE"] = FontAwesome.Sharp.IconChar.MinusCircle,
            ["UBL_EXCHANGERATE"]    = FontAwesome.Sharp.IconChar.ExchangeAlt,

            // ── UBL-TR e-Fatura — Ödeme ──
            ["UBL_PAYMENTMEANS"] = FontAwesome.Sharp.IconChar.CreditCard,
            ["UBL_PAYMENTTERMS"] = FontAwesome.Sharp.IconChar.CalendarAlt,

            // ── UBL-TR e-Fatura — Referanslar ──
            ["UBL_ORDERREF"]      = FontAwesome.Sharp.IconChar.Box,
            ["UBL_DESPATCHREF"]   = FontAwesome.Sharp.IconChar.Truck,
            ["UBL_BILLINGREF"]    = FontAwesome.Sharp.IconChar.FileInvoiceDollar,
            ["UBL_ADDITIONALDOC"] = FontAwesome.Sharp.IconChar.Paperclip,

            // ── UBL-TR e-Arşiv — Teslimat ──
            ["UBL_EA_DELIVERY"]     = FontAwesome.Sharp.IconChar.TruckLoading,
            ["UBL_EA_DELIVERYDATE"] = FontAwesome.Sharp.IconChar.CalendarCheck,
            ["UBL_EA_SHIPMENT"]     = FontAwesome.Sharp.IconChar.Ship,
            ["UBL_EA_CARRIER"]      = FontAwesome.Sharp.IconChar.TruckMoving,

            // ── UBL-TR e-Arşiv — E-Arşiv Özel ──
            ["UBL_EA_INTERNETSALES"]  = FontAwesome.Sharp.IconChar.Store,
            ["UBL_EA_SENDINGTYPE"]    = FontAwesome.Sharp.IconChar.PaperPlane,
            ["UBL_EA_PAYMENTCHANNEL"] = FontAwesome.Sharp.IconChar.University,
            ["UBL_EA_PAYMENTCODE"]    = FontAwesome.Sharp.IconChar.Receipt,
        };

        /// <summary>
        /// Kategori başlığına karşılık gelen FontAwesome ikonu.
        /// </summary>
        private static readonly Dictionary<string, FontAwesome.Sharp.IconChar> CategoryIconMap =
            new Dictionary<string, FontAwesome.Sharp.IconChar>
        {
            ["HTML Öğeleri"]    = FontAwesome.Sharp.IconChar.Html5,
            ["XSLT Komutları"]  = FontAwesome.Sharp.IconChar.Code,
            ["Sayfa Düzeni"]    = FontAwesome.Sharp.IconChar.Columns,
            ["UBL-TR e-Fatura"] = FontAwesome.Sharp.IconChar.FileInvoice,
            ["UBL-TR e-Arşiv"]  = FontAwesome.Sharp.IconChar.Archive,
        };

        /// <summary>
        /// Kategori adına göre renkli ikon rengini döndürür.
        /// </summary>
        private static Color GetCategoryColor(string category)
        {
            switch (category)
            {
                case "HTML Öğeleri":    return Color.FromArgb(33, 150, 243);   // Mavi
                case "XSLT Komutları":  return Color.FromArgb(156, 39, 176);   // Mor
                case "Sayfa Düzeni":    return Color.FromArgb(76, 175, 80);    // Yeşil
                case "UBL-TR e-Fatura": return Color.FromArgb(255, 87, 34);    // Turuncu
                case "UBL-TR e-Arşiv":  return Color.FromArgb(0, 137, 123);    // Deniz Yeşili
                default:                return Color.FromArgb(96, 125, 139);    // Gri-Mavi
            }
        }

        /// <summary>
        /// Snippet anahtarına göre FontAwesome vektörel renkli ikon oluşturur (16x16).
        /// </summary>
        private static Image CreateSnippetIcon(string snippetKey, Color color)
        {
            FontAwesome.Sharp.IconChar icon;
            if (!SnippetIconMap.TryGetValue(snippetKey, out icon))
                icon = FontAwesome.Sharp.IconChar.Circle;

            return icon.ToBitmap(color, 16);
        }

        /// <summary>
        /// Kategori / alt kategori başlığı için FontAwesome vektörel renkli ikon oluşturur (16x16).
        /// </summary>
        private static Image CreateCategoryIcon(string category, Color color)
        {
            FontAwesome.Sharp.IconChar icon;
            if (!CategoryIconMap.TryGetValue(category, out icon))
                icon = FontAwesome.Sharp.IconChar.FolderOpen;

            return icon.ToBitmap(color, 16);
        }

        #endregion

        private DevExpress.Utils.SuperToolTip CreateSnippetTooltip(SnippetInfo snippet)
        {
            var tip = new DevExpress.Utils.SuperToolTip();
            tip.MaxWidth = 600;
            tip.AllowHtmlText = DevExpress.Utils.DefaultBoolean.True;
            var titleItem = new DevExpress.Utils.ToolTipTitleItem { Text = snippet.DisplayName };
            var bodyItem = new DevExpress.Utils.ToolTipItem
            {
                Text = snippet.Description
            };
            tip.Items.Add(titleItem);
            tip.Items.Add(bodyItem);
            return tip;
        }

        /// <summary>
        /// Ribbon butonu tıklandığında XSLT editöründe imleç pozisyonuna snippet ekler.
        /// </summary>
        private void SnippetButton_ItemClick(object sender, ItemClickEventArgs e)
        {
            string key = e.Item.Tag as string;
            if (key == null || !XsltSnippets.Elements.ContainsKey(key))
                return;

            InsertSnippetAtCursor(XsltSnippets.Elements[key].XsltCode);
        }

        /// <summary>
        /// XSLT editöründe imleç pozisyonuna verilen kodu ekler.
        /// </summary>
        private void InsertSnippetAtCursor(string code)
        {
            var editor = textEditorControlEx1;
            var doc = editor.Document;
            var caret = editor.ActiveTextAreaControl.Caret;

            int offset = doc.PositionToOffset(caret.Position);
            doc.Insert(offset, code);
            editor.Refresh();

            // İmleci eklenen kodun sonuna taşı
            var newPos = doc.OffsetToPosition(offset + code.Length);
            caret.Position = newPos;

            UpdateAndCheckFoldings();
        }

        /// <summary>
        /// Ribbon üzerinde snippet butonuna mouse basılı tutulduğunda drag işlemini başlatır.
        /// </summary>
        private void RibbonControl_SnippetMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            var hitInfo = ribbonControl.CalcHitInfo(e.Location);
            if (hitInfo.Item == null || hitInfo.Item.Item == null) return;

            string key = hitInfo.Item.Item.Tag as string;
            if (key == null || !XsltSnippets.Elements.ContainsKey(key))
                return;

            _activeDragSnippetKey = key;
            var snippet = XsltSnippets.Elements[key];
            ribbonControl.DoDragDrop(snippet.DragDataString, DragDropEffects.Copy);
        }

        #region Faz 1 — XSLT Editörüne Drag-Drop

        /// <summary>
        /// XSLT editörünün drag-drop olaylarını bağlar.
        /// </summary>
        private void InitEditorDragDrop()
        {
            textEditorControlEx1.AllowDrop = true;
            textEditorControlEx1.ActiveTextAreaControl.TextArea.AllowDrop = true;
            textEditorControlEx1.ActiveTextAreaControl.TextArea.DragEnter += EditorTextArea_DragEnter;
            textEditorControlEx1.ActiveTextAreaControl.TextArea.DragDrop += EditorTextArea_DragDrop;
        }

        private void EditorTextArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text) ||
                e.Data.GetDataPresent(DataFormats.UnicodeText))
            {
                string data = e.Data.GetData(DataFormats.Text) as string ?? string.Empty;
                if (data.StartsWith(XsltSnippets.DragPrefix))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void EditorTextArea_DragDrop(object sender, DragEventArgs e)
        {
            string data = e.Data.GetData(DataFormats.Text) as string;
            if (string.IsNullOrEmpty(data) || !data.StartsWith(XsltSnippets.DragPrefix))
                return;

            string key = data.Substring(XsltSnippets.DragPrefix.Length);
            if (key.EndsWith(XsltSnippets.DragSuffix))
                key = key.Substring(0, key.Length - XsltSnippets.DragSuffix.Length);

            if (!XsltSnippets.Elements.ContainsKey(key))
                return;

            // Mouse pozisyonunu editör koordinatına çevir
            var textArea = textEditorControlEx1.ActiveTextAreaControl.TextArea;
            Point clientPoint = textArea.PointToClient(new Point(e.X, e.Y));

            var pos = textArea.TextView.GetLogicalPosition(
                Math.Max(0, clientPoint.X - textArea.TextView.DrawingPosition.X),
                Math.Max(0, clientPoint.Y - textArea.TextView.DrawingPosition.Y));

            var doc = textEditorControlEx1.Document;
            int offset = doc.PositionToOffset(pos);

            string code = XsltSnippets.Elements[key].XsltCode;
            doc.Insert(offset, code);
            textEditorControlEx1.Refresh();

            var newPos = doc.OffsetToPosition(offset + code.Length);
            textEditorControlEx1.ActiveTextAreaControl.Caret.Position = newPos;

            UpdateAndCheckFoldings();
        }

        #endregion
    }
}
