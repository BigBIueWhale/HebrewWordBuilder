using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.IO;

namespace HebrewWordbuilder
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        public class HebrewWord
        {
            public HebrewWord(string word, int numOccurences)
            {
                this.word = word;
                this.numOccurences = numOccurences;
            }
            public string word;
            public int numOccurences;
            public static string HebrewLetters = "אבגדהוזחטיכךלמםנןסעפףצץקרשת";
            public static bool contains_non_hebrew_non_space(string str_to_check)
            {
                foreach (char ch in str_to_check)
                {
                    if ((ch != ' ') && (!HebrewWord.HebrewLetters.Contains(ch)))
                        return true;
                }
                return false;
            }
            public static char convert_to_not_ending_letter(char ch)
            {
                if (ch == 'ך')
                    return 'כ';
                if (ch == 'ם')
                    return 'מ';
                if (ch == 'ן')
                    return 'נ';
                if (ch == 'ף')
                    return 'פ';
                if (ch == 'ץ')
                    return 'צ';
                return ch;
            }
        };
        private void button1_Click(object sender, EventArgs e)
        {
            const int max_num_of_words_for_result = 30000;
            button1.Enabled = false;
            bool differentiate_ending_letters = checkBox1.Checked;
            bool respect_non_hebrew_symbols = checkBox3.Checked;
            bool allow_using_letters_more_than_once = checkBox2.Checked;
            bool include_results_with_non_letters = checkBox4.Checked;
            int min_number_of_letters = (int)numericUpDown1.Value;
            int max_number_of_letters = (int)numericUpDown2.Value;
            if (min_number_of_letters > max_number_of_letters)
            {
                numericUpDown1.Value = 1;
                numericUpDown2.Value = 400;
                min_number_of_letters = (int)numericUpDown1.Value;
                max_number_of_letters = (int)numericUpDown2.Value;
            }
            label3.Text = "כאן יופיעו השגיאות";
            label3.ForeColor = Color.Blue;
            label3.BackColor = Color.Yellow;
            label3.Refresh();
            string input_data = textBox1.Text;
            List<HebrewWord> valid_words = new List<HebrewWord>();
            string textfile_path = "words.txt";
            if (File.Exists(textfile_path))
            {
                // Read a text file line by line.  
                string[] lines = File.ReadAllLines(textfile_path);
                int lines_counter = 0;
                foreach (string line in lines)
                {
                    int index_of_comma = line.IndexOf(",");
                    if (index_of_comma != -1)
                    {
                        bool was_error_in_parse = false;
                        int number_of_occurences = 0;
                        try
                        {
                            number_of_occurences = Int32.Parse(line.Substring(index_of_comma + 2, line.Length - (index_of_comma + 2)));
                        }
                        catch (FormatException)
                        {
                            was_error_in_parse = true;
                        }
                        if (!was_error_in_parse)
                        {
                            string current_word_str = line.Substring(0, index_of_comma);
                            HebrewWord current_word = new HebrewWord(current_word_str, number_of_occurences);
                            string input_data_cpy = "";
                            if (!differentiate_ending_letters)
                            {
                                for (int char_input_data_counter = 0; char_input_data_counter < input_data.Length; ++char_input_data_counter)
                                    input_data_cpy += HebrewWord.convert_to_not_ending_letter(input_data[char_input_data_counter]);
                            }
                            else
                                input_data_cpy = input_data;
                            bool word_is_valid = true;
                            while (current_word_str.Length > 0)
                            {
                                char current_char_in_word = current_word_str[0];
                                if (!differentiate_ending_letters)
                                    current_char_in_word = HebrewWord.convert_to_not_ending_letter(current_char_in_word);
                                if (respect_non_hebrew_symbols || HebrewWord.HebrewLetters.Contains(current_char_in_word))
                                {
                                    if (input_data_cpy.Contains(current_char_in_word))
                                    {
                                        if (!allow_using_letters_more_than_once)
                                            input_data_cpy = input_data_cpy.Remove(input_data_cpy.IndexOf(current_char_in_word), 1);
                                    }
                                    else
                                    {
                                        word_is_valid = false;
                                        break;
                                    }
                                }
                                current_word_str = current_word_str.Remove(0, 1);
                            }
                            if (word_is_valid)
                            {
                                if (valid_words.Count >= max_num_of_words_for_result)
                                {
                                    label3.Text = "שגיאה: יש יותר מדי תוצאות.";
                                    label3.ForeColor = Color.Green;
                                    label3.BackColor = Color.Pink;
                                    break;
                                }
                                if ((include_results_with_non_letters
                                    || (!HebrewWord.contains_non_hebrew_non_space(current_word.word)))
                                    && ((min_number_of_letters <= current_word.word.Length) && (max_number_of_letters >= current_word.word.Length)))
                                    valid_words.Add(current_word);
                            }
                        }
                        else
                        {
                            label3.Text = "שגיאה המספר בקובץ בשורה " + lines_counter.ToString() + " הוא לא תקין";
                            label3.ForeColor = Color.Green;
                            label3.BackColor = Color.Pink;
                            break;
                        }
                    }
                    ++lines_counter;
                }
                valid_words.Sort((a, b) => b.numOccurences.CompareTo(a.numOccurences));
                string text_of_answer = "";
                int num_valid_word_counter = 1;
                foreach (HebrewWord hebrew_word in valid_words)
                {
                    text_of_answer += num_valid_word_counter.ToString() + ". " + hebrew_word.word + "\r\n";
                    ++num_valid_word_counter;
                }
                if (valid_words.Count == 0)
                {
                    label3.Text = "אין תוצאות.";
                    label3.ForeColor = Color.Green;
                    label3.BackColor = Color.Pink;
                }
                textBox2.Text = text_of_answer;
            }
            else
            {
                string appPath = Application.StartupPath;
                label3.Text = "שגיאה: הקובץ words.txt לא נמצא בתיקייה" + "\r\n" + appPath;
                label3.ForeColor = Color.Green;
                label3.BackColor = Color.Pink;
                textBox2.Text = "תוצאה";
            }
            button1.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            label6.Enabled = true;
            label7.Enabled = true;
            checkBox1.Enabled = true;
            checkBox2.Enabled = true;
            checkBox3.Enabled = true;
            checkBox4.Enabled = true;
            numericUpDown1.Enabled = true;
            numericUpDown2.Enabled = true;
            label4.Enabled = true;
            label5.Enabled = true;
        }
    }
}
