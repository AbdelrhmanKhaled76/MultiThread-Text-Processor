using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace parallel_gui
{
    public partial class Form1 : Form
    {
        //our global variables
        int[] TableNumber = {0};
        string selectedFilePath;
        int totalWordCount = 0;
        object lockObj = new object();
        object lockObj2 = new object();
        Dictionary<string, int> wordFreq;
        
        public Form1()
        {
            InitializeComponent();
            button2.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
        //function to generate a text of random words
        //private string GenerateRandomText(int wordCount)
        //{
        //    // Array of random words
        //    string[] randomWords =
        //    {
        //        "apple", "banana", "cherry", "dog", "elephant", "forest", "grape",
        //        "house", "island", "jungle", "kite", "lemon", "mountain", "night",
        //        "orange", "pineapple", "queen", "river", "sun", "tree", "umbrella",
        //        "violet", "whale", "xylophone", "yellow", "zebra", "azure", "beauty",
        //        "cloud", "dream", "eagle", "freedom", "glory", "horizon", "ice",
        //        "jewel", "king", "love", "moon", "nature", "ocean", "peace",
        //        "quiet", "rain", "sky", "time", "universe", "vision", "wisdom"
        //    };

        //    string randomText = "";
        //    Random random = new Random();

        //    for (int i = 0; i < wordCount; i++)
        //    {
        //        int index = random.Next(randomWords.Length);
        //        randomText += randomWords[index] + " ";
        //    }

        //    return randomText.Trim();
        //}

        private int ProcessSection(string[] section, string searchWord, object lockObj, object lockObj2)
        {   
            int wordsCount = section.Length; // getting the number of words processed
            int choosenWordCount = 0;
            foreach (string word in section)
            {
                if(word == searchWord.ToLower())
                {
                    choosenWordCount++; // counting the occurrences for our search word per thread
                }

                lock (lockObj)
                {
                    if (wordFreq.ContainsKey(word)) //if word exist increase the value by 1
                    {
                        wordFreq[word]++;
                    }
                    else // else add the word with value equal to 1
                    {
                        wordFreq.Add(word, 1);
                    }
                } // counting the occurrences for every word            
            }
            lock (lockObj2) //locking the total counter to ensure there is no deadlocks
            {
                totalWordCount += choosenWordCount;
            }
            MessageBox.Show($"thread id : {Thread.CurrentThread.ManagedThreadId} ,number of words processed: {wordsCount} ,number of {searchWord} occurances : {choosenWordCount}") ;
            return choosenWordCount;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            string[] fileWords;
            int wordCount;
            int threadNum;

            //clearing the list box per process
            listBox1.Items.Clear();
            
            //clearing the total word counter per process
            totalWordCount = 0;

            //clearing the word frequency per proccess
            wordFreq = new Dictionary<string, int>();

            //getting the thread number from the input and validating it 
            if (!int.TryParse( textBox1.Text, out threadNum ) || textBox1.Text.Length == 0)
            {
                MessageBox.Show("please enter a valid number for the first text box !!");
                return;
            }
            
            //validating the second input and ensuring it isn't empty
            if(string.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("please enter a word or number for the second text box!!");
                return;
            }

            //validating the path and ensuring it isn't empty or corrupted
            if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                MessageBox.Show("please enter a valid document path");
                return;
            }

            // disabling start button after the validation until the program finishes
            start.Enabled = false;


            //MessageBox.Show($"{Thread.CurrentThread.ManagedThreadId}");

            try
            {
                //Reading the file using other thread with no punctuation and in lowerCase
                (fileWords, wordCount) = await Task.Run(async () =>
                {
                    //MessageBox.Show($"{Thread.CurrentThread.ManagedThreadId}");

                    using (StreamReader sr = new StreamReader(selectedFilePath))
                    {
                        string content = await sr.ReadToEndAsync();
                        fileWords = Regex.Matches(content, @"[a-zA-Z0-9]+").Cast<Match>().Select(m => m.Value.ToLower()).ToArray();
                        wordCount = fileWords.Length;
                        Invoke(new Action(() =>
                                listBox1.Items.Add($"file is found with word count of : {wordCount} word")
                        ));
                        return (fileWords, wordCount);
                    }
                });

                //else
                //{
                //    // File doesn't exist, generate random text and write it
                //    using (StreamWriter sw = new StreamWriter(selectedFilePath))
                //    {
                //        wordCount = 100000; 
                //        string fileText = GenerateRandomText(wordCount); // 100,000 words
                //        fileWords = fileText.Split(' ');
                //        await sw.WriteAsync(fileText);
                //        listBox1.Items.Add($"didn't find a valid File, file is created with word count of : {wordCount} word");
                //    }
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                return;
            }

            try
            {
                //calculating the number of words for every section
                int wordsPerSection = (int)Math.Ceiling((double)wordCount / threadNum);

                // inserting a message for user to know the steps of processing
                listBox1.Items.Add($"---------------------------------------------------------------------Processing {threadNum} sections...----------------------------------------------------------------------");



                //a delay for us to debug the process
                //await Task.Delay(2000);



                

                // Create a task for processing the section and awaiting it until the proccessing finish
                await Task.Run(() =>
                {
                    Parallel.For(0, threadNum, i =>
                    {
                        int start = i * wordsPerSection;

                        int end = (i == threadNum - 1) ? wordCount : start + wordsPerSection;
                        // 1,2,3,4,5,6  skip(2) take(3) 
                        int secOccurances = ProcessSection(fileWords.Skip(start).Take(end - start).ToArray(), textBox2.Text, lockObj, lockObj2);

                        // invoking the UI thread to make a change in the UI
                        Invoke(new Action(() =>
                            listBox1.Items.Add($"Section {i + 1} processed, occurrences of {textBox2.Text}: {secOccurances}")
                        ));
                    });

                });
                
                // Combine results in the listBox
                listBox1.Items.Add($"All sections processed, with total occurrences of the {textBox2.Text} equal to : {totalWordCount}");

                // message for user to know that the table is ready for displaying
                listBox1.Items.Add($"press on the table button if you want to see the full process !!");

                
                //enabling the button again after finishing the process
                start.Enabled = true;
                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                return;
            }

        }
        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }


        private void button1_Click_1(object sender, EventArgs e)
        {
            //making a button for getting the document path
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog.Title = "Select a file to process";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = openFileDialog.FileName;
                    button1.Text = $"Selected File: {selectedFilePath}";
                }
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private async void button2_Click(object sender, EventArgs e)
        {
            //closing the button from the main thread before creating the table from another one 
            button2.Enabled = false;

            // counting the table number with lock for more thread safty
            lock (TableNumber)
            {
                TableNumber[0]++;
            }

            //passing the button to control the button enabling , and passing the table number by reference for more control over the table number
            await Task.Run(() =>
            {
                Form2 activeTableForm = new Form2(wordFreq, button2, TableNumber);
                
                activeTableForm.ShowDialog(); // Show the form in the UI thread
            });
        }
    }
}
