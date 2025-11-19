using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CurrencyConverter_Database.OpenExchangeAPI;
using Newtonsoft.Json;

namespace CurrencyConverter_Database
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string _connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
        private int _currencyId = 0;
        private double _fromAmount = 0;
        private double _toAmount = 0;

        SqlConnection sqlConnection = new SqlConnection(_connectionString);
        SqlCommand sqlCommand = new SqlCommand();
        SqlDataAdapter sqlDataAdapter = new SqlDataAdapter();
        DataTable dtCurrency = new DataTable();
        public MainWindow()
        {
            InitializeComponent();
            _ = BindCurrency();
        }

        #region Binding Currency
        private async Task BindCurrency()
        {
            MessageBoxResult dialogResult = MessageBox.Show("Do you want to bind the currency data from database?", "Data Source", MessageBoxButton.YesNo);
            if (dialogResult == MessageBoxResult.Yes)
                BindCurrencyFromDB();
            else
            {
                dialogResult = MessageBox.Show("Do you want to bind the currency data onilne via API?", "Data Source", MessageBoxButton.YesNo);
                if (dialogResult == MessageBoxResult.Yes)
                {
                    await BindCurrencyViaAPI();
                }
                else
                    BindCurrencyLocal();
            }

            //// If we could not get data from API, we will get it localy
            //if (!await BindCurrencyViaAPI())
            //    BindCurrencyLocal();

        }

        private void BindCurrencyFromDB()
        {
            try
            {
                string query =
                    "SELECT *" +
                    "FROM [CurrencyExchangeRate]";
                sqlDataAdapter = new SqlDataAdapter(query, _connectionString);

                dtCurrency = new DataTable();
                sqlDataAdapter.Fill(dtCurrency);
                dtCurrency.PrimaryKey = new DataColumn[] { dtCurrency.Columns["Id"] };

                FillDataTableIntoGUI(dtCurrency);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
            }

        }

        private void BindCurrencyLocal()
        {
            // Create DataTable
            dtCurrency = new DataTable();

            // Add columns to the DataTable
            dtCurrency.Columns.Add("Id");
            dtCurrency.Columns.Add("CurrencyName");
            dtCurrency.Columns.Add("Amount");

            int idCounter = 0;
            // Add rows in the DataTable with columns mentioned above
            //dtCurrency.Rows.Add("--SELECT--", 0);
            dtCurrency.Rows.Add(idCounter++, "INR", 1);
            dtCurrency.Rows.Add(idCounter++, "USD", 75);
            dtCurrency.Rows.Add(idCounter++, "EUR", 85);
            dtCurrency.Rows.Add(idCounter++, "SAR", 20);
            dtCurrency.Rows.Add(idCounter++, "POUND", 5);
            dtCurrency.Rows.Add(idCounter++, "DEM", 43);

            dtCurrency.PrimaryKey = new DataColumn[] { dtCurrency.Columns["Id"] };

            FillDataTableIntoGUI(dtCurrency);
        }

        private async Task<bool> BindCurrencyViaAPI()
        {
            #region frank furter API
            //// if device offline
            //bool isOnline = NetworkInterface.GetIsNetworkAvailable();
            //if (!isOnline)
            //    return false;


            //using (var client = new HttpClient())
            //{
            //    string url = "https://api.frankfurter.dev/v1/latest"; // Default base = EUR

            //    client.Timeout = TimeSpan.FromSeconds(3);
            //    var testConnectionResponse = await client.GetAsync(url);
            //    if (testConnectionResponse.IsSuccessStatusCode)
            //    {
            //        var response = await client.GetStringAsync(url);
            //        var json = JsonDocument.Parse(response);

            //        string baseCurrency = json.RootElement.GetProperty("base").GetString();
            //        string date = json.RootElement.GetProperty("date").GetString();

            //        Console.WriteLine($"Base Currency: {baseCurrency}");
            //        DateLabel.Content = date;

            //        var rates = json.RootElement.GetProperty("rates");


            //        // Create DataTable
            //        dtCurrency = new DataTable();

            //        // Add columns to the DataTable
            //        dtCurrency.Columns.Add("Id");
            //        dtCurrency.Columns.Add("CurrencyName");
            //        dtCurrency.Columns.Add("Amount");

            //        int idCounter = 0;
            //        // Add rows in the DataTable with columns mentioned above
            //        //dtCurrency.Rows.Add(idCounter++, "--SELECT--", 0);
            //        dtCurrency.Rows.Add(idCounter++, "EUR", 1);

            //        foreach (var rate in rates.EnumerateObject())
            //        {
            //            dtCurrency.Rows.Add(idCounter++, rate.Name, rate.Value.GetDecimal());
            //        }

            //        FillDataTableIntoGUI(dtCurrency);

            //        dtCurrency.PrimaryKey = new DataColumn[] { dtCurrency.Columns["Id"] };

            //    }
            //    return testConnectionResponse.IsSuccessStatusCode;
            //}
            #endregion

            #region Open Exchange API
            // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
            string app_id = "143d4bff846d432a8c5231193107d927";
            string url = $"https://openexchangerates.org/api/latest.json?app_id={app_id}";
            Root root = new Root();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(1);
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string responseString = await response.Content.ReadAsStringAsync();
                        var responseObject = JsonConvert.DeserializeObject<Root>(responseString);


                        // Create DataTable
                        dtCurrency = new DataTable();

                        // Add columns to the DataTable
                        dtCurrency.Columns.Add("Id");
                        dtCurrency.Columns.Add("CurrencyName");
                        dtCurrency.Columns.Add("Amount");

                        int idCounter = 0;
                        // Add rows in the DataTable with columns mentioned above
                        //dtCurrency.Rows.Add(idCounter++, "--SELECT--", 0);
                        foreach (var rate in responseObject.rates)
                        {
                            dtCurrency.Rows.Add(idCounter++, rate.Key, rate.Value);
                        }

                        FillDataTableIntoGUI(dtCurrency);

                        dtCurrency.PrimaryKey = new DataColumn[] { dtCurrency.Columns["Id"] };

                        MessageBox.Show($"TimeStamp: {responseObject.timestamp}");
                        return true;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }

            #endregion
        }
        #endregion
        private void FillDataTableIntoGUI(DataTable dtCurrency)
        {
            cmbFromCurrency.ItemsSource = dtCurrency.DefaultView;
            cmbFromCurrency.DisplayMemberPath = "CurrencyName";
            cmbFromCurrency.SelectedValuePath = "Amount";
            cmbFromCurrency.SelectedIndex = 0;

            cmbToCurrency.ItemsSource = dtCurrency.DefaultView;
            cmbToCurrency.DisplayMemberPath = "CurrencyName";
            cmbToCurrency.SelectedValuePath = "Amount";
            cmbToCurrency.SelectedIndex = 0;

            // Remove "--SELECT--" Row
            //dtCurrency.Rows.RemoveAt(0);
            dgvCurrency.ItemsSource = dtCurrency.DefaultView;
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {//Create the variable as ConvertedValue with double datatype to store currency converted value
            double ConvertedValue;

            //Check if the amount textbox is Null or Blank
            if (txtCurrencyAmount.Text == null || txtCurrencyAmount.Text.Trim() == "")
            {
                //If amount textbox is Null or Blank it will show this message box
                MessageBox.Show("Please Enter Currency", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                //After clicking on messagebox OK set focus on amount textbox
                txtCurrencyAmount.Focus();
                return;
            }
            //Else if currency From is not selected or select default text --SELECT--
            else if (cmbFromCurrency.SelectedValue == null)
            {
                //Show the message
                MessageBox.Show("Please Select Currency From", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                //Set focus on the From Combobox
                cmbFromCurrency.Focus();
                return;
            }
            //Else if currency To is not selected or select default text --SELECT--
            else if (cmbToCurrency.SelectedValue == null)
            {
                //Show the message
                MessageBox.Show("Please Select Currency To", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                //Set focus on the To Combobox
                cmbToCurrency.Focus();
                return;
            }

            //Check if From and To Combobox selected values are same
            if (cmbFromCurrency.Text == cmbToCurrency.Text)
            {
                //Amount textbox value set in ConvertedValue.
                //double.parse is used for converting the datatype String To Double.
                //Textbox text have string and ConvertedValue is double Datatype
                ConvertedValue = double.Parse(txtCurrencyAmount.Text);

                //Show the label converted currency and converted currency name and ToString("N3") is used to place 000 after the dot(.)
                lblCurrency.Content = cmbToCurrency.Text + " " + ConvertedValue.ToString("N3");
            }
            else
            {
                //Calculation for currency converter is From Currency value multiply(*) 
                //With the amount textbox value and then that total divided(/) with To Currency value
                ConvertedValue = (double.Parse(cmbToCurrency.SelectedValue.ToString()) * double.Parse(txtCurrencyAmount.Text)) / double.Parse(cmbFromCurrency.SelectedValue.ToString());

                //Show the label converted currency and converted currency name.
                lblCurrency.Content = cmbToCurrency.Text + " " + ConvertedValue.ToString("N3");
            }


        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            lblCurrency.Content = string.Empty;
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Regex allow numbers + one decimal point
            Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
            TextBox currentTextBox = sender as TextBox;
            string newText = currentTextBox.Text.Insert(currentTextBox.SelectionStart, e.Text);
            bool isMatch = regex.IsMatch(newText);
            e.Handled = !isMatch;
        }

        private void txtCurrency_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string pastedText = e.DataObject.GetData(typeof(string)).ToString();
                Regex regex = new Regex(@"^[0-9]*(?:\.[0-9]*)?$");
                TextBox currentTextBox = sender as TextBox;
                string newText = currentTextBox.Text.Insert(currentTextBox.SelectionStart, pastedText);

                if (!regex.IsMatch(newText))
                    e.CancelCommand();
            }
            else
                e.CancelCommand();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(txtAmount.Text))
                {
                    MessageBox.Show("Enter amount", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtAmount.Focus();
                    return;
                }
                else if (string.IsNullOrEmpty(txtCurrencyName.Text))
                {
                    MessageBox.Show("Enter currency name", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtCurrencyName.Focus();
                    return;
                }
                else
                {
                    // Get _currencyId from dtCurrency
                    _currencyId = dtCurrency.AsEnumerable()
                        .Where(currencyRow => currencyRow.Field<string>("CurrencyName") == txtCurrencyName.Text)
                        .Select(currencyRow => currencyRow.Field<int>("Id")).FirstOrDefault();

                    sqlConnection.Open();
                    if (_currencyId > 0)
                    {
                        DataRow updatedRow = dtCurrency.Rows.Find(_currencyId);

                        if (MessageBox.Show($"Are you sure you want to update currency {txtCurrencyName.Text} to be with new amount {txtAmount.Text} instead of old amount {updatedRow["Amount"]}?", "Update", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            sqlCommand = new SqlCommand(
                                "UPDATE [dbo].[CurrencyExchangeRate]\r\n " +
                                "SET " +
                                "[Amount] = @Amount " +
                                //", [CurrencyName] = @CurrencyName\r\n " +
                                "WHERE Id = @Id;\r\n ", sqlConnection);

                            sqlCommand.Parameters.AddWithValue("@Amount", txtAmount.Text);
                            //sqlCommand.Parameters.AddWithValue("@CurrencyName", txtCurrencyName.Text);
                            sqlCommand.Parameters.AddWithValue("@Id", _currencyId);

                            sqlCommand.ExecuteNonQuery();


                            // عدّل الصف في الـ DataTable
                            if (updatedRow != null)
                                updatedRow["Amount"] = double.Parse(txtAmount.Text);

                            MessageBox.Show($"Currency {txtCurrencyName.Text} amount has been updated successfully updated to be {txtAmount.Text}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    else
                    {
                        if (MessageBox.Show("This currency does not exist. Do you want to add it as a new Currency?", "Add Currency", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            sqlCommand = new SqlCommand(
                                "INSERT INTO [dbo].[CurrencyExchangeRate]\r\n" +
                                "([Amount],[CurrencyName])\r\n" +
                                "VALUES\r\n" +
                                "(@Amount, @CurrencyName);\r\n" +
                                "SELECT SCOPE_IDENTITY();\r\n", sqlConnection);

                            sqlCommand.Parameters.AddWithValue("@Amount", txtAmount.Text);
                            sqlCommand.Parameters.AddWithValue("@CurrencyName", txtCurrencyName.Text);

                            var newId = Convert.ToInt32(sqlCommand.ExecuteScalar());

                            MessageBox.Show($"Currency {txtCurrencyName.Text} has added successfully with amount equal {txtAmount.Text}", "Information", MessageBoxButton.OK, MessageBoxImage.Information);


                            // أضف نفس الصف للـ DataTable المحلي
                            DataRow newRow = dtCurrency.NewRow();
                            newRow["Id"] = newId;
                            newRow["Amount"] = txtAmount.Text;
                            newRow["CurrencyName"] = txtCurrencyName.Text;
                            dtCurrency.Rows.Add(newRow);

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                ClearMaster();
                sqlConnection.Close();
            }
        }

        private void ClearMaster()
        {

            txtAmount.Clear();
            txtCurrencyName.Clear();
            txtAmount.Focus();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            ClearMaster();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView selectedDataGridRow = dgvCurrency.CurrentItem as DataRowView;
                if (selectedDataGridRow != null && dgvCurrency.Items.Count > 0 && dtCurrency.Rows.Count > 0 && dgvCurrency.SelectedCells.Count > 0)
                {
                    _currencyId = (int)selectedDataGridRow["Id"];
                    if (dgvCurrency.SelectedCells[0].Column.Header.ToString() == "Delete")
                    {
                        string currencyName = selectedDataGridRow["CurrencyName"].ToString();

                        if (MessageBox.Show($"Are you sure you want to delete currency {currencyName}?", "Delete", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            sqlConnection.Open();

                            sqlCommand = new SqlCommand(
                                "DELETE FROM [dbo].[CurrencyExchangeRate]\r\n " +
                                "WHERE Id = @Id;\r\n ", sqlConnection);

                            sqlCommand.Parameters.AddWithValue("@Id", _currencyId);


                            sqlCommand.ExecuteNonQuery();

                            dtCurrency.Rows.Remove(dtCurrency.Rows.Find(_currencyId));

                            MessageBox.Show($"Currency {currencyName} has been updated successfully deleted", "Information", MessageBoxButton.OK, MessageBoxImage.Information);

                        }
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView selectedDataGridRow = dgvCurrency.CurrentItem as DataRowView;
                if (selectedDataGridRow != null && dgvCurrency.Items.Count > 0 && dtCurrency.Rows.Count > 0 && dgvCurrency.SelectedCells.Count > 0)
                {
                    _currencyId = (int)selectedDataGridRow["Id"];
                    if (dgvCurrency.SelectedCells[0].Column.Header.ToString() == "Edit")
                    {
                        MessageBox.Show("Edit");
                    }
                }
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                sqlConnection.Close();
            }
        }
    }
}
