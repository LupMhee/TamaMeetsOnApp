using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TamagotchiMeetsOnEditor;

namespace TamagotchiMeetsOnEditor
{
    public partial class MainForm : Form
    {
        private List<FirmwareImage> foundImages = new List<FirmwareImage>();
        private byte[]? firmwareData = null;

        private ListBox imageListBox = null!;
        private Label imageInfoLabel = null!;
        private PictureBox previewPictureBox = null!;
        private ProgressBar progressBar = null!;
        private Label statusLabel = null!;
        private Panel progressPanel = null!;
        private Button replaceImageButton = null!;
        private FirmwareImage? selectedImage = null;
        private TextBox addressSearchBox = null!;
        private List<FirmwareImage> allImages = new List<FirmwareImage>();
        private Panel palettePanel = null!;
        private TextBox paletteHexTextBox = null!;
        private RichTextBox imageHexTextBox = null!;
        private ToolTip paletteToolTip = new ToolTip();

        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Tamagotchi Meets/On Firmware Editor";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(800, 600);


            progressPanel = new Panel();
            progressPanel.Dock = DockStyle.Top;
            progressPanel.Height = 60;
            progressPanel.Visible = false;
            progressPanel.BackColor = Color.LightGray;

            statusLabel = new Label();
            statusLabel.Text = "Loading...";
            statusLabel.Dock = DockStyle.Top;
            statusLabel.Height = 25;
            statusLabel.Padding = new Padding(5);
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;

            progressBar = new ProgressBar();
            progressBar.Dock = DockStyle.Fill;
            progressBar.Style = ProgressBarStyle.Marquee;

            progressPanel.Controls.Add(progressBar);
            progressPanel.Controls.Add(statusLabel);
            this.Controls.Add(progressPanel);


            MenuStrip menuStrip = new MenuStrip();
            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem openItem = new ToolStripMenuItem("Open Firmware...", null, OpenFirmware_Click);
            openItem.ShortcutKeys = Keys.Control | Keys.O;


             ToolStripMenuItem extractAllImagesItem = new ToolStripMenuItem("Extract All Images...", null, ExtractAllImages_Click);

            ToolStripMenuItem saveFirmwareItem = new ToolStripMenuItem("Save Firmware As...", null, SaveFirmware_Click);
            saveFirmwareItem.ShortcutKeys = Keys.Control | Keys.S;
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit", null, (s, e) => Application.Exit());
             fileMenu.DropDownItems.AddRange(new ToolStripItem[] { 
                 openItem, 


                 new ToolStripSeparator(), 
                 extractAllImagesItem,

                 new ToolStripSeparator(),
                 saveFirmwareItem,
                 new ToolStripSeparator(), 
                 exitItem 
             });
            menuStrip.Items.Add(fileMenu);
            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);


            TabControl tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            

            TabPage imagesTab = new TabPage("Images");
            imagesTab.Controls.Add(CreateImageListPanel());
            tabControl.TabPages.Add(imagesTab);

            this.Controls.Add(tabControl);
        }

        private Panel CreateImageListPanel()
        {
            SplitContainer splitContainer = new SplitContainer();
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.FixedPanel = FixedPanel.None; 
            splitContainer.SplitterDistance = 50; 


            Panel leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Fill;


            Panel searchPanel = new Panel();
            searchPanel.Dock = DockStyle.Top;
            searchPanel.Height = 50;
            searchPanel.Padding = new Padding(5);

            Label searchLabel = new Label();
            searchLabel.Text = "Search Address:";
            searchLabel.Location = new Point(5, 15);
            searchLabel.AutoSize = true;

            addressSearchBox = new TextBox();
            addressSearchBox.Location = new Point(100, 12);
            addressSearchBox.Width = 180;
            addressSearchBox.PlaceholderText = "0x12345678 or 12345678";
            addressSearchBox.TextChanged += AddressSearchBox_TextChanged;

            Button clearSearchButton = new Button();
            clearSearchButton.Text = "Clear";
            clearSearchButton.Location = new Point(285, 11);
            clearSearchButton.Width = 60;
            clearSearchButton.Height = 23;
            clearSearchButton.Click += (s, e) => { addressSearchBox.Text = ""; };

            searchPanel.Controls.Add(searchLabel);
            searchPanel.Controls.Add(addressSearchBox);
            searchPanel.Controls.Add(clearSearchButton);

            Label label = new Label();
            label.Text = "Found Images:";
            label.Dock = DockStyle.Top;
            label.Height = 25;
            label.Padding = new Padding(5);

            imageListBox = new ListBox();
            imageListBox.Dock = DockStyle.Fill;
            imageListBox.DisplayMember = "DisplayText";
            imageListBox.SelectedIndexChanged += ImageListBox_SelectedIndexChanged;
            
            leftPanel.Controls.Add(imageListBox);
            leftPanel.Controls.Add(label);
            leftPanel.Controls.Add(searchPanel);
            splitContainer.Panel1.Controls.Add(leftPanel);


            splitContainer.Panel2.Controls.Add(CreateImagePreviewPanel());

            Panel container = new Panel();
            container.Dock = DockStyle.Fill;
            container.Controls.Add(splitContainer);
            return container;
        }

        private Panel CreateImagePreviewPanel()
        {

            SplitContainer mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Orientation = Orientation.Horizontal;
            mainSplit.FixedPanel = FixedPanel.None; 
            mainSplit.SplitterDistance = 50; 


            Panel topPanel = new Panel();
            topPanel.Dock = DockStyle.Fill;

            imageInfoLabel = new Label();
            imageInfoLabel.Dock = DockStyle.Top;
            imageInfoLabel.Height = 80;
            imageInfoLabel.Padding = new Padding(5);
            imageInfoLabel.AutoSize = false;


            Panel buttonPanel = new Panel();
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.Height = 40;
            buttonPanel.Padding = new Padding(5);

            replaceImageButton = new Button();
            replaceImageButton.Text = "Replace Image...";
            replaceImageButton.Height = 30;
            replaceImageButton.Width = 150;
            replaceImageButton.Location = new Point(5, 5);
            replaceImageButton.Click += ReplaceImage_Click;
            replaceImageButton.Enabled = false;

            buttonPanel.Controls.Add(replaceImageButton);

            previewPictureBox = new PictureBox();
            previewPictureBox.Dock = DockStyle.Fill;
            previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            previewPictureBox.BackColor = Color.White;

            topPanel.Controls.Add(previewPictureBox);
            topPanel.Controls.Add(buttonPanel);
            topPanel.Controls.Add(imageInfoLabel);


            SplitContainer bottomSplit = new SplitContainer();
            bottomSplit.Dock = DockStyle.Fill;
            bottomSplit.Orientation = Orientation.Vertical;
            bottomSplit.FixedPanel = FixedPanel.None; 
            bottomSplit.SplitterDistance = 50; 


            Panel palettePanelContainer = new Panel();
            palettePanelContainer.Dock = DockStyle.Fill;
            palettePanelContainer.Padding = new Padding(5);

            Label paletteLabel = new Label();
            paletteLabel.Text = "Palette:";
            paletteLabel.Dock = DockStyle.Top;
            paletteLabel.Height = 25;
            paletteLabel.AutoSize = false;

            palettePanel = new Panel();
            palettePanel.Dock = DockStyle.Top;
            palettePanel.Height = 60;
            palettePanel.AutoScroll = true;
            palettePanel.BackColor = Color.White;
            palettePanel.BorderStyle = BorderStyle.FixedSingle;

            Label paletteHexLabel = new Label();
            paletteHexLabel.Text = "Palette Hex Data:";
            paletteHexLabel.Dock = DockStyle.Top;
            paletteHexLabel.Height = 25;
            paletteHexLabel.AutoSize = false;

            paletteHexTextBox = new TextBox();
            paletteHexTextBox.Dock = DockStyle.Fill;
            paletteHexTextBox.Multiline = true;
            paletteHexTextBox.ReadOnly = true;
            paletteHexTextBox.Font = new Font("Consolas", 9);
            paletteHexTextBox.ScrollBars = ScrollBars.Vertical;

            Panel paletteTextContainer = new Panel();
            paletteTextContainer.Dock = DockStyle.Fill;
            paletteTextContainer.Controls.Add(paletteHexTextBox);
            paletteTextContainer.Controls.Add(paletteHexLabel);

            palettePanelContainer.Controls.Add(paletteTextContainer);
            palettePanelContainer.Controls.Add(palettePanel);
            palettePanelContainer.Controls.Add(paletteLabel);


            Panel imageHexContainer = new Panel();
            imageHexContainer.Dock = DockStyle.Fill;
            imageHexContainer.Padding = new Padding(5);

            Label imageHexLabel = new Label();
            imageHexLabel.Text = "Image Hex Data:";
            imageHexLabel.Dock = DockStyle.Top;
            imageHexLabel.Height = 25;
            imageHexLabel.AutoSize = false;

            imageHexTextBox = new RichTextBox();
            imageHexTextBox.Dock = DockStyle.Fill;
            imageHexTextBox.ReadOnly = true;
            imageHexTextBox.Font = new Font("Consolas", 9);
            imageHexTextBox.ScrollBars = RichTextBoxScrollBars.Both;
            imageHexTextBox.WordWrap = false;

            imageHexContainer.Controls.Add(imageHexTextBox);
            imageHexContainer.Controls.Add(imageHexLabel);

            bottomSplit.Panel1.Controls.Add(palettePanelContainer);
            bottomSplit.Panel2.Controls.Add(imageHexContainer);

            mainSplit.Panel1.Controls.Add(topPanel);
            mainSplit.Panel2.Controls.Add(bottomSplit);

            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Controls.Add(mainSplit);
            return panel;
        }


        private void OpenFirmware_Click(object? sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Firmware Files (*.bin)|*.bin|All Files (*.*)|*.*";
                dialog.Title = "Open Tamagotchi Firmware File";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    LoadFirmware(dialog.FileName);
                }
            }
        }

        private async void LoadFirmware(string filePath)
        {
            try
            {

                this.Cursor = Cursors.WaitCursor;
                this.Text = $"Tamagotchi Meets/On Firmware Editor - {Path.GetFileName(filePath)}";
                progressPanel.Visible = true;
                statusLabel.Text = "Loading firmware file...";
                Application.DoEvents();


                byte[]? data = null;
                await Task.Run(() =>
                {
                    data = File.ReadAllBytes(filePath);
                });

                if (data == null)
                {
                    throw new Exception("Failed to load firmware file");
                }

                firmwareData = data;
                statusLabel.Text = $"Firmware loaded ({data.Length:N0} bytes). Scanning for images...";
                Application.DoEvents();


                List<FirmwareImage>? images = null;
                await Task.Run(() =>
                {
                    images = FirmwareImageScanner.ScanForImages(firmwareData);
                });

                if (images != null)
                {
                    foundImages = images;
                    allImages = new List<FirmwareImage>(images); 
                    if (imageListBox.InvokeRequired)
                    {
                        imageListBox.Invoke((MethodInvoker)(() =>
                        {
                            UpdateImageList();
                        }));
                    }
                    else
                    {
                        UpdateImageList();
                    }
                }

                if (this.InvokeRequired)
                {
                    this.Invoke((MethodInvoker)(() =>
                    {
                        statusLabel.Text = $"Found {foundImages.Count} images.";
                    }));
                }
                else
                {
                    statusLabel.Text = $"Found {foundImages.Count} images.";
                }
                Application.DoEvents();










































                {











                }


                progressPanel.Visible = false;
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Found {foundImages.Count} images in firmware.", 
                    "Scan Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                progressPanel.Visible = false;
                this.Cursor = Cursors.Default;
                MessageBox.Show($"Error loading firmware: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddressSearchBox_TextChanged(object? sender, EventArgs e)
        {
            UpdateImageList();
        }

        private void UpdateImageList()
        {
            if (addressSearchBox == null || allImages == null)
            {
                return;
            }

            string searchText = addressSearchBox.Text.Trim();
            List<FirmwareImage> filteredImages;

            if (string.IsNullOrEmpty(searchText))
            {

                filteredImages = new List<FirmwareImage>(allImages);
            }
            else
            {

                int? searchOffset = ParseAddress(searchText);
                if (searchOffset.HasValue)
                {

                    filteredImages = allImages
                        .Where(img => img.Offset == searchOffset.Value)
                        .ToList();
                    

                    if (filteredImages.Count == 0)
                    {
                        var closest = allImages
                            .OrderBy(img => Math.Abs(img.Offset - searchOffset.Value))
                            .Take(10)
                            .ToList();
                        filteredImages = closest;
                    }
                }
                else
                {

                    string searchLower = searchText.ToLower();
                    filteredImages = allImages
                        .Where(img => img.DisplayText.ToLower().Contains(searchLower))
                        .ToList();
                }
            }

            foundImages = filteredImages;
            imageListBox.DataSource = null;
            imageListBox.DataSource = foundImages;
            imageListBox.DisplayMember = "DisplayText";
        }

        private int? ParseAddress(string text)
        {
            text = text.Trim();
            

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(2);
            }


            if (int.TryParse(text, System.Globalization.NumberStyles.HexNumber, null, out int hexValue))
            {
                return hexValue;
            }


            if (int.TryParse(text, out int decValue))
            {
                return decValue;
            }

            return null;
        }

        private void ImageListBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (imageListBox?.SelectedItem is FirmwareImage image)
            {
                selectedImage = image;
                imageInfoLabel.Text = $"Offset: 0x{image.Offset:X8}\n" +
                                   $"Size: {image.Size} bytes\n" +
                                   $"Dimensions: {image.Width} × {image.Height}\n" +
                                   $"Colors: {image.ColorsCount}";

                previewPictureBox.Image = image.GetBitmap();
                

                UpdatePaletteDisplay(image);
                

                UpdateHexDataDisplays(image);
                

                if (replaceImageButton != null)
                {
                    replaceImageButton.Enabled = true;
                }
            }
            else
            {
                selectedImage = null;
                ClearPaletteAndHexDisplays();
                if (replaceImageButton != null)
                {
                    replaceImageButton.Enabled = false;
                }
            }
        }

        private void UpdatePaletteDisplay(FirmwareImage image)
        {
            if (palettePanel == null) return;

            palettePanel.Controls.Clear();
            palettePanel.Refresh(); 
            
            Color[] palette = image.Palette;
            int swatchSize = 30;
            int spacing = 5;
            int startX = spacing;
            int startY = spacing;
            int currentX = startX;
            int currentY = startY;
            int maxWidth = palettePanel.Width > 0 ? palettePanel.Width : 300;
            

            if (maxWidth <= 0 && palettePanel.Parent != null)
            {
                maxWidth = palettePanel.Parent.Width - 20; 
            }
            if (maxWidth <= 0)
            {
                maxWidth = 400; 
            }

            for (int i = 0; i < palette.Length; i++)
            {
                Panel swatch = new Panel();
                swatch.Size = new Size(swatchSize, swatchSize);
                swatch.BackColor = palette[i];
                swatch.BorderStyle = BorderStyle.FixedSingle;
                swatch.Location = new Point(currentX, currentY);
                
                Color c = palette[i];
                string colorInfo = $"Index: {i}\nRGB: ({c.R}, {c.G}, {c.B})\nHex: #{c.R:X2}{c.G:X2}{c.B:X2}";
                paletteToolTip.SetToolTip(swatch, colorInfo);

                palettePanel.Controls.Add(swatch);

                currentX += swatchSize + spacing;
                if (currentX + swatchSize > maxWidth - spacing)
                {
                    currentX = startX;
                    currentY += swatchSize + spacing;
                }
            }

            int itemsPerRow = Math.Max(1, (maxWidth - spacing) / (swatchSize + spacing));
            int rows = (int)Math.Ceiling((double)palette.Length / itemsPerRow);
            palettePanel.Height = Math.Max(60, rows * (swatchSize + spacing) + spacing);
            
            palettePanel.Refresh();
        }

        private void UpdateHexDataDisplays(FirmwareImage image)
        {
            if (paletteHexTextBox == null || imageHexTextBox == null) return;

            byte[] imageData = image.ImageData;
            
            int paletteOffset = 6;
            int paletteLength = image.ColorsCount * 2;
            if (imageData.Length >= paletteOffset + paletteLength)
            {
                byte[] paletteData = new byte[paletteLength];
                Array.Copy(imageData, paletteOffset, paletteData, 0, paletteLength);
                paletteHexTextBox.Text = BytesToHexString(paletteData, 16);
            }
            else
            {
                paletteHexTextBox.Text = "";
            }

            imageHexTextBox.Clear();
            
            bool halfBytePixel = image.ColorsCount <= 16;
            int pixelDataOffset = 6 + image.ColorsCount * 2;
            int width = image.Width;
            int height = image.Height;
            Color[] palette = image.Palette;
            
            int headerPaletteLength = pixelDataOffset;
            if (imageData.Length >= headerPaletteLength)
            {
                string headerPaletteHex = BytesToHexString(imageData.Take(headerPaletteLength).ToArray(), 16);
                imageHexTextBox.AppendText(headerPaletteHex);
                if (headerPaletteLength < imageData.Length)
                {
                    imageHexTextBox.AppendText("\n");
                }
            }
            
            if (halfBytePixel)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        int byteIndex = pixelDataOffset + (y * width + x) / 2;
                        if (byteIndex < imageData.Length)
                        {
                            byte byteData = imageData[byteIndex];
                            int pixel1Index = byteData & 0x0F;
                            
                            if (pixel1Index < palette.Length)
                            {
                                Color pixelColor = palette[pixel1Index];
                                imageHexTextBox.SelectionStart = imageHexTextBox.TextLength;
                                imageHexTextBox.SelectionLength = 0;
                                imageHexTextBox.SelectionColor = pixelColor;
                                imageHexTextBox.AppendText(byteData.ToString("X2"));
                                imageHexTextBox.SelectionColor = Color.Black;
                            }
                            else
                            {
                                imageHexTextBox.AppendText(byteData.ToString("X2"));
                            }
                            
                            int bytesInRow = (width + 1) / 2;
                            if ((x / 2) < bytesInRow - 1)
                            {
                                imageHexTextBox.AppendText(" ");
                            }
                        }
                    }
                    
                    if (y < height - 1)
                    {
                        imageHexTextBox.AppendText("\n");
                    }
                }
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int byteIndex = pixelDataOffset + y * width + x;
                        if (byteIndex < imageData.Length)
                        {
                            int colorIndex = imageData[byteIndex];
                            if (colorIndex < palette.Length)
                            {
                                Color pixelColor = palette[colorIndex];
                                imageHexTextBox.SelectionStart = imageHexTextBox.TextLength;
                                imageHexTextBox.SelectionLength = 0;
                                imageHexTextBox.SelectionColor = pixelColor;
                                imageHexTextBox.AppendText(imageData[byteIndex].ToString("X2"));
                                imageHexTextBox.SelectionColor = Color.Black;
                            }
                            
                            if (x < width - 1)
                            {
                                imageHexTextBox.AppendText(" ");
                            }
                        }
                    }
                    
                    if (y < height - 1)
                    {
                        imageHexTextBox.AppendText("\n");
                    }
                }
            }
        }

        private void ClearPaletteAndHexDisplays()
        {
            if (palettePanel != null)
            {
                palettePanel.Controls.Clear();
            }
            if (paletteHexTextBox != null)
            {
                paletteHexTextBox.Text = "";
            }
            if (imageHexTextBox != null)
            {
                imageHexTextBox.Text = "";
            }
        }

        private string BytesToHexString(byte[] data, int bytesPerLine)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                if (i > 0 && i % bytesPerLine == 0)
                {
                    sb.AppendLine();
                }
                else if (i > 0)
                {
                    sb.Append(" ");
                }
                sb.Append(data[i].ToString("X2"));
            }
            return sb.ToString();
        }

        private async void ReplaceImage_Click(object? sender, EventArgs e)
        {
            if (selectedImage == null || firmwareData == null)
            {
                MessageBox.Show("Please select an image first.", "No Image Selected", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image Files (*.png;*.bmp;*.jpg;*.jpeg;*.gif)|*.png;*.bmp;*.jpg;*.jpeg;*.gif|All Files (*.*)|*.*";
                dialog.Title = "Select Replacement Image";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        this.Cursor = Cursors.WaitCursor;
                        progressPanel.Visible = true;
                        statusLabel.Text = "Processing replacement image...";
                        Application.DoEvents();

                        Bitmap? newBitmap = null;
                        await Task.Run(() =>
                        {
                            using (var tempBitmap = new Bitmap(dialog.FileName))
                            {
                                newBitmap = new Bitmap(tempBitmap, selectedImage.Width, selectedImage.Height);
                            }
                        });

                        if (newBitmap == null)
                        {
                            throw new Exception("Failed to load replacement image");
                        }

                        Bitmap? originalBitmap = null;
                        await Task.Run(() =>
                        {
                            originalBitmap = selectedImage.GetBitmap();
                        });
                        
                        bool isSameImage = false;
                        if (originalBitmap != null && newBitmap != null && 
                            originalBitmap.Width == newBitmap.Width && 
                            originalBitmap.Height == newBitmap.Height)
                        {
                            int differentPixels = 0;
                            int totalPixels = originalBitmap.Width * originalBitmap.Height;
                            for (int y = 0; y < originalBitmap.Height; y++)
                            {
                                for (int x = 0; x < originalBitmap.Width; x++)
                                {
                                    Color origColor = originalBitmap.GetPixel(x, y);
                                    Color newColor = newBitmap.GetPixel(x, y);
                                    if (origColor.R != newColor.R || origColor.G != newColor.G || origColor.B != newColor.B)
                                    {
                                        differentPixels++;
                                    }
                                }
                            }
                            double differenceRatio = (double)differentPixels / totalPixels;
                            isSameImage = differenceRatio < 0.01;
                        }
                        
                        byte[] newImageData;
                        if (isSameImage && newBitmap != null)
                        {
                            newImageData = new byte[selectedImage.Size];
                            if (firmwareData != null && selectedImage.Offset + selectedImage.Size <= firmwareData.Length)
                            {
                                Array.Copy(firmwareData, selectedImage.Offset, newImageData, 0, selectedImage.Size);
                                Array.Copy(newImageData, 0, firmwareData, selectedImage.Offset, newImageData.Length);
                            }
                            else
                            {
                                newImageData = (byte[])selectedImage.ImageData.Clone();
                                if (firmwareData != null)
                                {
                                    Array.Copy(newImageData, 0, firmwareData, selectedImage.Offset, newImageData.Length);
                                }
                            }
                            
                            selectedImage.UpdateImageData(newImageData);
                            previewPictureBox.Image = selectedImage.GetBitmap();
                            UpdatePaletteDisplay(selectedImage);
                            UpdateHexDataDisplays(selectedImage);
                        }
                        else if (newBitmap != null)
                        {
                            newImageData = await Task.Run(() =>
                            {
                                return ConvertImageToFirmwareFormat(newBitmap, selectedImage);
                            });

                            selectedImage.UpdateImageData(newImageData);
                            Array.Copy(newImageData, 0, firmwareData, selectedImage.Offset, newImageData.Length);
                            previewPictureBox.Image = selectedImage.GetBitmap();
                            UpdatePaletteDisplay(selectedImage);
                            UpdateHexDataDisplays(selectedImage);
                        }
                        else
                        {
                            throw new Exception("Failed to process replacement image");
                        }

                        imageListBox.Refresh();

                        progressPanel.Visible = false;
                        this.Cursor = Cursors.Default;

                        MessageBox.Show("Image replaced successfully!", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        progressPanel.Visible = false;
                        this.Cursor = Cursors.Default;
                        MessageBox.Show($"Error replacing image: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private byte[] ConvertImageToFirmwareFormat(Bitmap bitmap, FirmwareImage originalImage)
        {
            int targetSize = originalImage.Size;
            int targetPaletteSize = originalImage.ColorsCount;
            bool use4Bit = targetPaletteSize <= 16;

            var originalPalette = new List<Color>();
            int paletteDataOffset = originalImage.Offset + 6;
            for (int i = 0; i < targetPaletteSize; i++)
            {
                if (firmwareData != null && paletteDataOffset + (i * 2) + 1 < firmwareData.Length)
                {
                    ushort color16 = (ushort)((firmwareData[paletteDataOffset + (i * 2)] << 8) | firmwareData[paletteDataOffset + (i * 2) + 1]);
                    int blue = (int)Math.Round((((color16 & 0xF800) >> 11) / 31.0) * 255);
                    int green = (int)Math.Round((((color16 & 0x07E0) >> 5) / 63.0) * 255);
                    int red = (int)Math.Round(((color16 & 0x001F) / 31.0) * 255);
                    originalPalette.Add(Color.FromArgb(255, red, green, blue));
                }
                else
                {
                    originalPalette.Add(Color.Black);
                }
            }

            bool useOriginalPalette = true;
            var originalIndexedImage = new byte[bitmap.Width * bitmap.Height];
            int dataOffset = 6 + targetPaletteSize * 2;
            
            if (use4Bit)
            {
                int maxPixels = bitmap.Width * bitmap.Height;
                int pixelIndex = 0;
                for (int i = 0; i < maxPixels / 2 && firmwareData != null && originalImage.Offset + dataOffset + i < firmwareData.Length; i++)
                {
                    byte byteData = firmwareData[originalImage.Offset + dataOffset + i];
                    int pixel1Index = byteData & 0x0F;
                    int pixel2Index = (byteData >> 4) & 0x0F;
                    
                    if (pixelIndex < maxPixels)
                    {
                        int x1 = pixelIndex % bitmap.Width;
                        int y1 = pixelIndex / bitmap.Width;
                        Color pixelColor = bitmap.GetPixel(x1, y1);
                        Color paletteColor = pixel1Index < originalPalette.Count ? originalPalette[pixel1Index] : Color.Black;
                        double distance = ColorDistance(pixelColor, paletteColor);
                        if (distance > 50)
                        {
                            useOriginalPalette = false;
                            break;
                        }
                        originalIndexedImage[pixelIndex] = (byte)pixel1Index;
                    }
                    pixelIndex++;
                    
                    if (pixelIndex < maxPixels)
                    {
                        int x2 = pixelIndex % bitmap.Width;
                        int y2 = pixelIndex / bitmap.Width;
                        Color pixelColor = bitmap.GetPixel(x2, y2);
                        Color paletteColor = pixel2Index < originalPalette.Count ? originalPalette[pixel2Index] : Color.Black;
                        double distance = ColorDistance(pixelColor, paletteColor);
                        if (distance > 50)
                        {
                            useOriginalPalette = false;
                            break;
                        }
                        originalIndexedImage[pixelIndex] = (byte)pixel2Index;
                    }
                    pixelIndex++;
                }
            }
            else
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        int index = y * bitmap.Width + x;
                        if (firmwareData != null && originalImage.Offset + dataOffset + index < firmwareData.Length)
                        {
                            int colorIndex = firmwareData[originalImage.Offset + dataOffset + index];
                            Color pixelColor = bitmap.GetPixel(x, y);
                            Color paletteColor = colorIndex < originalPalette.Count ? originalPalette[colorIndex] : Color.Black;
                            double distance = ColorDistance(pixelColor, paletteColor);
                            if (distance > 50)
                            {
                                useOriginalPalette = false;
                                break;
                            }
                            originalIndexedImage[index] = (byte)colorIndex;
                        }
                    }
                    if (!useOriginalPalette) break;
                }
            }

            List<Color> newPalette = new List<Color>();
            byte[] tempIndexedImage = new byte[bitmap.Width * bitmap.Height];
            
            if (!useOriginalPalette)
            {
                var colorQuantizer = new ColorQuantizer();
                var quantized = colorQuantizer.Quantize(bitmap, targetPaletteSize);
                newPalette = quantized?.Palette ?? new List<Color>();
                tempIndexedImage = quantized?.IndexedImage ?? new byte[bitmap.Width * bitmap.Height];
            }

            var palette = new List<Color>();
            
            if (useOriginalPalette)
            {
                palette = new List<Color>(originalPalette);
                var originalIndexedImg = new byte[originalIndexedImage.Length];
                Array.Copy(originalIndexedImage, originalIndexedImg, originalIndexedImage.Length);
                
                Color transColor = Color.FromArgb(0, 255, 0);
                int transIndex = -1;
                
                for (int i = 0; i < palette.Count; i++)
                {
                    Color c = palette[i];
                    if (c.G >= 240 && c.R < 50 && c.B < 50)
                    {
                        transIndex = i;
                        break;
                    }
                }
                
                if (transIndex > 0)
                {
                    Color transparentCol = palette[transIndex];
                    Color colorAt0 = palette[0];
                    palette[0] = transparentCol;
                    palette[transIndex] = colorAt0;
                    
                    for (int i = 0; i < originalIndexedImg.Length; i++)
                    {
                        if (originalIndexedImg[i] == transIndex)
                        {
                            originalIndexedImg[i] = 0;
                        }
                        else if (originalIndexedImg[i] == 0)
                        {
                            originalIndexedImg[i] = (byte)transIndex;
                        }
                    }
                }
                
                byte[] resultData = new byte[targetSize];
                int resultOffset = 0;

                resultData[resultOffset++] = (byte)originalImage.Width;
                resultData[resultOffset++] = (byte)originalImage.Height;
                resultData[resultOffset++] = (byte)targetPaletteSize;
                resultData[resultOffset++] = 0;
                resultData[resultOffset++] = 1;
                resultData[resultOffset++] = 255;

                for (int i = 0; i < targetPaletteSize; i++)
                {
                    Color color = i < palette.Count ? palette[i] : Color.Black;
                    
                    if (i == 0 && transIndex >= 0)
                    {
                        color = Color.FromArgb(0, 255, 0);
                    }
                    
                    ushort color16 = EncodeRGB565(color);
                    resultData[resultOffset++] = (byte)((color16 >> 8) & 0xFF);
                    resultData[resultOffset++] = (byte)(color16 & 0xFF);
                }

                if (use4Bit)
                {
                    for (int i = 0; i < (originalImage.Width * originalImage.Height) / 2; i++)
                    {
                        int pixelIndex1 = i * 2;
                        int pixelIndex2 = i * 2 + 1;
                        
                        byte pixel1 = pixelIndex1 < originalIndexedImg.Length ? (byte)(originalIndexedImg[pixelIndex1] & 0x0F) : (byte)0;
                        byte pixel2 = pixelIndex2 < originalIndexedImg.Length ? (byte)(originalIndexedImg[pixelIndex2] & 0x0F) : (byte)0;
                        
                        resultData[resultOffset++] = (byte)(pixel1 | (pixel2 << 4));
                    }
                }
                else
                {
                    for (int i = 0; i < originalImage.Width * originalImage.Height; i++)
                    {
                        resultData[resultOffset++] = i < originalIndexedImg.Length ? originalIndexedImg[i] : (byte)0;
                    }
                }

                if (resultData.Length != targetSize)
                {
                    byte[] resized = new byte[targetSize];
                    int copyLength = Math.Min(resultData.Length, targetSize);
                    Array.Copy(resultData, 0, resized, 0, copyLength);
                    return resized;
                }

                return resultData;
            }
            
            for (int i = 0; i < newPalette.Count && palette.Count < targetPaletteSize; i++)
            {
                palette.Add(newPalette[i]);
            }

            while (palette.Count < targetPaletteSize)
            {
                palette.Add(palette.Count > 0 ? palette[palette.Count - 1] : Color.Black);
            }

            Color transparentColor = Color.FromArgb(0, 255, 0);
            int transparentIndex = -1;
            
            for (int i = 0; i < palette.Count; i++)
            {
                Color c = palette[i];
                if (c.G >= 240 && c.R < 50 && c.B < 50)
                {
                    transparentIndex = i;
                    break;
                }
            }
            
            if (transparentIndex > 0)
            {
                Color transparentCol = palette[transparentIndex];
                Color colorAt0 = palette[0];
                palette[0] = transparentCol;
                palette[transparentIndex] = colorAt0;
                
                for (int i = 0; i < tempIndexedImage.Length; i++)
                {
                    if (tempIndexedImage[i] == transparentIndex)
                    {
                        tempIndexedImage[i] = 0;
                    }
                    else if (tempIndexedImage[i] == 0)
                    {
                        tempIndexedImage[i] = (byte)transparentIndex;
                    }
                }
            }
            else if (transparentIndex == -1 && originalPalette.Count > 0)
            {
                Color originalColor0 = originalPalette[0];
                if (originalColor0.G >= 240 && originalColor0.R < 50 && originalColor0.B < 50)
                {
                    palette[0] = transparentColor;
                }
            }

            var indexedImage = tempIndexedImage;
            
            if (palette.Count != targetPaletteSize)
            {
                while (palette.Count < targetPaletteSize)
                {
                    palette.Add(palette.Count > 0 ? palette[palette.Count - 1] : Color.Black);
                }
            }

            int headerSize = 6;
            int paletteSize = targetPaletteSize * 2;
            int pixelDataSize;
            
            if (use4Bit)
            {
                pixelDataSize = (int)Math.Ceiling((originalImage.Width * originalImage.Height) / 2.0);
            }
            else
            {
                pixelDataSize = originalImage.Width * originalImage.Height;
            }

            int calculatedSize = headerSize + paletteSize + pixelDataSize;
            int targetTotalSize = targetSize;

            byte[] result = new byte[targetTotalSize];
            int offset = 0;

            result[offset++] = (byte)originalImage.Width;
            result[offset++] = (byte)originalImage.Height;
            result[offset++] = (byte)targetPaletteSize;
            result[offset++] = 0;
            result[offset++] = 1;
            result[offset++] = 255;

            Color palette0Color = palette.Count > 0 ? palette[0] : Color.Black;
            bool hasTransparent = palette0Color.G >= 240 && palette0Color.R < 50 && palette0Color.B < 50;
            
            for (int i = 0; i < targetPaletteSize; i++)
            {
                Color color = i < palette.Count ? palette[i] : Color.Black;
                
                if (i == 0 && hasTransparent)
                {
                    color = Color.FromArgb(0, 255, 0);
                }
                
                ushort color16 = EncodeRGB565(color);
                result[offset++] = (byte)((color16 >> 8) & 0xFF);
                result[offset++] = (byte)(color16 & 0xFF);
            }

            if (use4Bit)
            {
                for (int i = 0; i < pixelDataSize; i++)
                {
                    int pixelIndex1 = i * 2;
                    int pixelIndex2 = i * 2 + 1;
                    
                    byte pixel1 = pixelIndex1 < indexedImage.Length ? (byte)(indexedImage[pixelIndex1] & 0x0F) : (byte)0;
                    byte pixel2 = pixelIndex2 < indexedImage.Length ? (byte)(indexedImage[pixelIndex2] & 0x0F) : (byte)0;
                    
                    result[offset++] = (byte)(pixel1 | (pixel2 << 4));
                }
            }
            else
            {
                for (int i = 0; i < pixelDataSize; i++)
                {
                    result[offset++] = i < indexedImage.Length ? indexedImage[i] : (byte)0;
                }
            }

            if (result.Length != originalImage.Size)
            {
                byte[] resized = new byte[originalImage.Size];
                int copyLength = Math.Min(result.Length, originalImage.Size);
                Array.Copy(result, 0, resized, 0, copyLength);
                
                if (copyLength < originalImage.Size)
                {
                    for (int i = copyLength; i < originalImage.Size; i++)
                    {
                        resized[i] = 0;
                    }
                }
                
                return resized;
            }

            return result;
        }

        private ushort EncodeRGB565(Color color)
        {
            int b = (color.B * 31) / 255;
            int g = (color.G * 63) / 255;
            int r = (color.R * 31) / 255;
            return (ushort)(((b & 0x1F) << 11) | ((g & 0x3F) << 5) | (r & 0x1F));
        }

        private double ColorDistance(Color c1, Color c2)
        {
            int dr = c1.R - c2.R;
            int dg = c1.G - c2.G;
            int db = c1.B - c2.B;
            return Math.Sqrt(dr * dr + dg * dg + db * db);
        }

        private async void SaveFirmware_Click(object? sender, EventArgs e)
        {
            if (firmwareData == null)
            {
                MessageBox.Show("Please load a firmware file first.", "No Firmware Loaded", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Firmware Files (*.bin)|*.bin|All Files (*.*)|*.*";
                dialog.Title = "Save Firmware As...";
                dialog.DefaultExt = "bin";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        this.Cursor = Cursors.WaitCursor;
                        progressPanel.Visible = true;
                        statusLabel.Text = "Saving firmware...";
                        Application.DoEvents();

                        await Task.Run(() =>
                        {
                            File.WriteAllBytes(dialog.FileName, firmwareData);
                        });

                        progressPanel.Visible = false;
                        this.Cursor = Cursors.Default;

                        MessageBox.Show($"Firmware saved successfully to:\n{dialog.FileName}", "Success", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        progressPanel.Visible = false;
                        this.Cursor = Cursors.Default;
                        MessageBox.Show($"Error saving firmware: {ex.Message}", "Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void ExtractAllImages_Click(object? sender, EventArgs e)
        {
            if (foundImages.Count == 0)
            {
                MessageBox.Show("No images found. Please load a firmware file first.", "No Images", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select folder to save extracted images";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ExtractAllImages(dialog.SelectedPath);
                }
            }
        }

        private void ExtractAllImages(string outputFolder)
        {
            int saved = 0;
            int failed = 0;
            
            foreach (var image in foundImages)
            {
                try
                {
                    string filename = $"image_{image.Offset:X8}_{image.Width}x{image.Height}.png";
                    string filepath = Path.Combine(outputFolder, filename);
                    image.GetBitmap().Save(filepath, ImageFormat.Png);
                    saved++;
                }
                catch (Exception ex)
                {
                    failed++;
                    System.Diagnostics.Debug.WriteLine($"Error saving image at offset 0x{image.Offset:X8}: {ex.Message}");
                }
            }
            
            string message = $"Successfully extracted {saved} of {foundImages.Count} images.";
            if (failed > 0)
            {
                message += $"\n{failed} images failed to save.";
            }
            
            MessageBox.Show(message, "Extraction Complete", MessageBoxButtons.OK, 
                failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
    }
}
