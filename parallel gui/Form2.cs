using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace parallel_gui
{
    public partial class Form2 : Form
    {
        private Dictionary<string, int> wordFreq;
        Button TableButton;
        int[] tableNumber;
        public Form2(Dictionary<string,int> freqWord, Button button,int[] tableNum) // initializing needed variables from the first form in the constructor
        { 
            InitializeComponent();
            wordFreq = freqWord;
            tableNumber = tableNum; 
            TableButton = button;
            this.FormClosing += Form2_FormClosing;
            //MessageBox.Show($"{Thread.CurrentThread.ManagedThreadId}");
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e) //initializing form closing event for enabling the table button on close
        {
            //on closing decrement the table number 
            lock (tableNumber)
            {
                tableNumber[0]--;
            }

            //enabling the table button when no other tables are active
            if (tableNumber[0] == 0)
            {
                Invoke(new Action(() =>
                {
                    TableButton.Enabled = true;
                }));
            }
        }

        protected async override void OnLoad(EventArgs e) //initializing the load button to add rows to the corresponding table on load
        {
            base.OnLoad(e);

            const int chunkSize = 100; // Adjust chunk size as needed

            int processed = 0;

            // Process the word frequency data in chunks
            List<KeyValuePair<string, int>> wordList = wordFreq.ToList();

            while (processed < wordList.Count) // deviding the array into chunks to add the rows without making th UI hang
            {
                int remaining = wordList.Count - processed;
                int currentChunkSize = Math.Min(chunkSize, remaining);

                var currentChunk = wordList.Skip(processed).Take(currentChunkSize);
                foreach (var wordOcc in currentChunk)
                {
                    dataGridView1.Rows.Add(wordOcc.Key, wordOcc.Value);
                }

                processed += currentChunkSize;

                // Allow the UI to refresh and remain responsive
                await Task.Delay(1);
            }

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //making a search textbox for the user to search of any specific words in real time
            try
            {
                //validating the search textbox
                if (string.IsNullOrEmpty(textBox1.Text))
                {
                    MessageBox.Show("please enter a valid word");
                    return;
                }
                //printing a message for user incase the word is not in our dictionary
                if (!wordFreq.ContainsKey(textBox1.Text))
                {
                    MessageBox.Show($"The word '{textBox1.Text}' is not in the dataset.");
                    return;
                }
                //printing the result
                MessageBox.Show($"the word {textBox1.Text} has total number occurences of : {wordFreq[textBox1.Text]}");
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void Form2_Load_1(object sender, EventArgs e)
        {

        }
    }
}
