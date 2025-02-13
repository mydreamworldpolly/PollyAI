using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Control = System.Windows.Controls.Control;
using FontFamily = System.Windows.Media.FontFamily;

namespace PollyAI5
{

    public partial class MainWindow : Window
    {

        BitmapImage nullImage = new BitmapImage();
        AI ai = new AI("Act as a helpful assistant","api.json");
        

        Copilot copilot = new Copilot();
        string memory = "";
        string prompt = "";

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = ai;
            List<string> a = ai.GetAllModelNames();
            modelBox.ItemsSource = ai.GetAllModelNames();
            ImageModelBox.ItemsSource = GenImage.GetAllModelNames();
            maxtokenBox.Text = ai.GetModelFromName(modelBox.Text).outputLength.ToString();
            GenImage.model = GenImage.GetAllModelNames()[0];
        }

        private void Window_Loaded(object sender, EventArgs e)
        {
            Set_font();
            LoadTxtFiles();
        }


        private async void Sendbutton_Click(object sender, RoutedEventArgs e)
        {
            string originalContent = Sendbutton.Content.ToString();
            try
            {
                Sendbutton.IsEnabled = false;

                Sendbutton.Content = "wait...";


                string systemPromptOrigin = ai.DialogEntries[0].DialogText;

                if (prompt != "")
                {
                    ai.DialogEntries[0].DialogText = prompt;
                }
                if (memory != "")
                {
                    ai.DialogEntries[0].DialogText = ai.DialogEntries[0].DialogText +
                        $"Note: When solving problems, the following supplementary knowledge is considered to be knowledge you have already mastered：<{memory}>";
                }

                // Remove empty DialogText items
                var itemsToRemove = ai.DialogEntries.Where(entry => string.IsNullOrEmpty(entry.DialogText)).ToList();
                foreach (var item in itemsToRemove)
                {
                    ai.DialogEntries.Remove(item);
                }

                await ai.Chat(Convert.ToInt32(maxtokenBox.Text), (float)creativeBar.Value / 10, modelBox.Text);

                ai.DialogEntries[0].DialogText = systemPromptOrigin;
            }
            finally
            {
                Sendbutton.IsEnabled = true;
                Sendbutton.Content = originalContent;

            }
        }

    private async void CopilotButton_Click(object sender, RoutedEventArgs e)
    {
        string originalContent = CopilotButton.Content.ToString();
        try 
        {
            CopilotButton.IsEnabled = false;
            
            CopilotButton.Content = "wait...";

            string systemPromptOrigin = ai.DialogEntries[0].DialogText;

            if (prompt != "")
            {
                ai.DialogEntries[0].DialogText = prompt;
            }
            if (memory != "")
            {
                ai.DialogEntries[0].DialogText = ai.DialogEntries[0].DialogText + 
                    $"Note: When solving problems, the following supplementary knowledge is considered to be knowledge you have already mastered：<{memory}>";
            }

            // Remove empty DialogText items
            var itemsToRemove = ai.DialogEntries.Where(entry => string.IsNullOrEmpty(entry.DialogText)).ToList();
            foreach (var item in itemsToRemove)
            {
                ai.DialogEntries.Remove(item);
            }

            try
            {
                    Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Render, new Action(() => { }));// Force UI update
                    await Dispatcher.Yield(DispatcherPriority.Render);
                    await copilot.Work(Convert.ToInt32(maxtokenBox.Text), (float)creativeBar.Value / 10, modelBox.Text, ai, memory);
            }
            catch (Exception ex)
            {
                MessageLabel.Content = ex.Message;
            }

            ai.DialogEntries[0].DialogText = systemPromptOrigin;
        }
        finally 
        {
            CopilotButton.IsEnabled = true;
            CopilotButton.Content = originalContent;
        }
    }

        

        private void Clearbutton_Click(object sender, RoutedEventArgs e)
        {
            ai.DialogEntries.Clear();
            ai.DialogEntries.Add(new DialogEntry
            {
                Character = "system",
                DialogText = "Act as a helpful assistant",
                Image = null
            });
        }

        private void Helpbutton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("1. You should manage the conversation yourself. For instance, you can delete unnecessary historical responses to save tokens." + "\r" +
                "2. To delete a line, select it and press 'Delete'. To edit a cell, double-click it. " + "\r" +
                "3. The first line should be reserved for system messages, such as 'Acting as a writer.'" + "\r" +
                "4. Draw button and its options are used to generate paintings. Prompt would be the latest data in the conversation." +
                "It could be expensive so think twice and make it count. The painting will save on the desktop named {current date and time}.png"
                );
        }

        private void FontBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Set_font();
        }

        void Set_font()
        {
            Style newStyle = new Style(typeof(DataGridCell));
            newStyle.Setters.Add(new Setter(Control.FontFamilyProperty, new FontFamily("微软雅黑")));
            newStyle.Setters.Add(new Setter(Control.FontSizeProperty, fontBar.Value));
            dialogDataGrid.Columns[1].CellStyle = newStyle;
        }

        private void dialogDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid != null && e.Row.DataContext != CollectionView.NewItemPlaceholder)
            {
                e.Row.Header = (e.Row.GetIndex() + 1).ToString();
            }
            else
            {
                e.Row.Header = "*";
            }
        }

        private void DialogDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var cellInfo = dialogDataGrid.CurrentCell;
            DataGrid dataGrid = sender as DataGrid;
            if (cellInfo.Column.Header.ToString() == "Image")
            {
                DialogEntry dialogEntry = cellInfo.Item as DialogEntry;


                var fileDialog = new OpenFileDialog();
                fileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";
                if (fileDialog.ShowDialog() == true)
                {

                    // if new line，then create new DialogEntry
                    if (dialogEntry.DialogText == null && dialogEntry.Image == null)
                    {
                        dialogEntry.DialogText = "";
                    }

                    dialogEntry.Image = new Bitmap(fileDialog.FileName);
                    dialogDataGrid.CommitEdit(); // end edit
                }
            }
        }

        private void DialogDataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid != null)
                {
                    dataGrid.CommitEdit(DataGridEditingUnit.Cell, true); // end cell edit
                    dataGrid.CommitEdit(DataGridEditingUnit.Row, true);  // end line edit
                    e.Handled = true; // change enter key activity
                }
            }
        }

        private void MemoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleSelectionChanged(MemoryBox, ref memory);
        }

        private void HandleSelectionChanged(ComboBox comboBox, ref string targetVariable)
        {
            if (comboBox.SelectedItem == null) return;

            string selectedItem = (comboBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                                  ?? comboBox.SelectedItem.ToString();

            if (selectedItem.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                targetVariable = string.Empty;
            }
            else
            {
                string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, selectedItem + ".txt");
                if (File.Exists(filePath))
                {
                    targetVariable = File.ReadAllText(filePath);
                }
                else
                {
                    MessageBox.Show($"File not found: {filePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadTxtFiles()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var txtFiles = Directory.GetFiles(exeDirectory, "*.txt")
                                    .Select(Path.GetFileNameWithoutExtension)
                                    .ToList();
            foreach (var file in txtFiles)
            {
                MemoryBox.Items.Add(file);
                PromptBox.Items.Add(file);
            }
            PromptBox.SelectedIndex = 0;
            MemoryBox.SelectedIndex = 0;
        }

        private void modelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string selectedItem = (modelBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                                  ?? modelBox.SelectedItem.ToString();
            AIModel selected = ai.GetModelFromName(selectedItem);
            if (maxtokenBox != null)
            {
                maxtokenBox.Text = selected.outputLength.ToString();
            }
        }

        private void ImageModelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GenImage.model = (ImageModelBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                                  ?? ImageModelBox.SelectedItem.ToString();
        }

        private void PromptBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleSelectionChanged(PromptBox, ref prompt);
        }
    }


}
