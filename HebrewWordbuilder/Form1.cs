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
using System.Threading;

namespace HebrewWordbuilder
{
    public partial class Form1 : Form
    {
        public static bool tracking_input_length = false;
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
            public static string beginning_letters = "כמנפצ";
            public static bool contains_non_hebrew_non_space(string str_to_check)
            {
                foreach (char ch in str_to_check)
                {
                    if ((ch != ' ') && (!HebrewWord.HebrewLetters.Contains(ch)))
                        return true;
                }
                return false;
            }
            public static int count_hebrew_letters(string str_to_count)
            {
                int counter = 0;
                foreach (char ch in str_to_count)
                {
                    if (HebrewWord.HebrewLetters.Contains(ch))
                        ++counter;
                }
                return counter;
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
            public static int count_dictionary_values(Dictionary<string, int> dic)
            {
                int sum = 0;
                foreach (KeyValuePair<string, int> word in dic)
                {
                    sum += word.Value;
                }
                return sum;
            }
            public static int compare_dictionaries_based_on_occurence(Dictionary<string, int> dic1, Dictionary<string, int> dic2)
            {
                return HebrewWord.count_dictionary_values(dic1).CompareTo(HebrewWord.count_dictionary_values(dic2));
            }
        };
        private class ContainsEverythingNeeded
        {
            public ContainsEverythingNeeded(int min_number_of_letters, bool respect_non_hebrew_symbols, bool differentiate_ending_letters, bool allow_using_letters_more_than_once, bool build_full_sentences, bool include_results_with_non_letters, string input_data, CancellationToken ct)
            {
                this.min_number_of_letters = min_number_of_letters;
                this.respect_non_hebrew_symbols = respect_non_hebrew_symbols;
                this.differentiate_ending_letters = differentiate_ending_letters;
                this.allow_using_letters_more_than_once = allow_using_letters_more_than_once;
                this.build_full_sentences = build_full_sentences;
                this.include_results_with_non_letters = include_results_with_non_letters;
                this.input_data = input_data;
                this.ct = ct;
            }
            public CancellationToken ct;
            public int min_number_of_letters;
            public bool respect_non_hebrew_symbols;
            public bool differentiate_ending_letters;
            public bool allow_using_letters_more_than_once;
            public bool build_full_sentences;
            public bool include_results_with_non_letters;
            public string input_data;
        }
        private static CancellationTokenSource cts;
        private Thread thread;
        private void start_thread(object obj)
        {
            thread = new Thread(() =>
            {
                ContainsEverythingNeeded vars = (ContainsEverythingNeeded)obj;
                const int max_num_of_words_for_result = 30000;
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
                                if (!vars.differentiate_ending_letters)
                                {
                                    for (int char_input_data_counter = 0; char_input_data_counter < vars.input_data.Length; ++char_input_data_counter)
                                        input_data_cpy += HebrewWord.convert_to_not_ending_letter(vars.input_data[char_input_data_counter]);
                                }
                                else
                                    input_data_cpy = vars.input_data;
                                bool word_is_valid = true;
                                while (current_word_str.Length > 0)
                                {
                                    char current_char_in_word = current_word_str[0];
                                    if (!vars.differentiate_ending_letters)
                                        current_char_in_word = HebrewWord.convert_to_not_ending_letter(current_char_in_word);
                                    if (vars.respect_non_hebrew_symbols || HebrewWord.HebrewLetters.Contains(current_char_in_word))
                                    {
                                        if (input_data_cpy.Contains(current_char_in_word))
                                        {
                                            if (!vars.allow_using_letters_more_than_once)
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
                                    if ((valid_words.Count >= max_num_of_words_for_result) && (!vars.build_full_sentences))
                                    {
                                        label3.Invoke(new DisplayThingsDelegate(display_error), "שגיאה: יש יותר מדי תוצאות.");
                                        break;
                                    }
                                    bool condition_for_word;
                                    if (vars.build_full_sentences)
                                        condition_for_word = true;
                                    else
                                    {
                                        if (vars.respect_non_hebrew_symbols)
                                            condition_for_word = vars.min_number_of_letters <= current_word.word.Length;
                                        else
                                            condition_for_word = vars.min_number_of_letters <= HebrewWord.count_hebrew_letters(current_word.word);
                                    }
                                    char last_char = current_word.word[current_word.word.Length - 1];
                                    if (HebrewWord.beginning_letters.Contains(last_char))
                                        condition_for_word = false;
                                    if ((vars.include_results_with_non_letters
                                        || (!HebrewWord.contains_non_hebrew_non_space(current_word.word)))
                                        && condition_for_word)
                                        valid_words.Add(current_word);
                                }
                            }
                            else
                            {
                                label3.Invoke(new DisplayThingsDelegate(display_error), "שגיאה המספר בקובץ בשורה " + lines_counter.ToString() + " הוא לא תקין");
                                break;
                            }
                        }
                        ++lines_counter;
                    }
                    valid_words.Sort((a, b) => b.numOccurences.CompareTo(a.numOccurences));
                    string text_of_answer = "";
                    if (valid_words.Count == 0)
                        label3.Invoke(new DisplayThingsDelegate(display_error), "שגיאה: אין תוצאות.");
                    else
                    {
                        if (!vars.build_full_sentences)
                        {
                            int num_valid_word_counter = 1;
                            foreach (HebrewWord hebrew_word in valid_words)
                            {
                                if (num_valid_word_counter < valid_words.Count)
                                    text_of_answer += num_valid_word_counter.ToString() + ". " + hebrew_word.word + "\r\n";
                                else
                                    text_of_answer += num_valid_word_counter.ToString() + ". " + hebrew_word.word;
                                ++num_valid_word_counter;
                            }
                        }
                        else
                        {
                            button3.Invoke(new SetEnabled(set_button3_enabled), true);
                            label5.Invoke(new SetVisible(set_label5_visible), true);
                            List<Dictionary<string, int>> sentences_without_word_order = new List<Dictionary<string, int>>();
                            const int max_num_of_sentences = 10000;
                            List<int> word_indexes_that_make_sentence = new List<int>();
                            word_indexes_that_make_sentence.Add(0);
                            int level_at_which_sentence_is_too_short = 0;
                            int previous_num_of_sentences_until_now = 0;
                            DateTime start_time = DateTime.UtcNow;
                            while (word_indexes_that_make_sentence.Count > 0)
                            {
                                if (previous_num_of_sentences_until_now != sentences_without_word_order.Count)
                                {
                                    DateTime end_time = DateTime.UtcNow;
                                    TimeSpan timeDiff = end_time - start_time;
                                    if (timeDiff.TotalMilliseconds > 1000.0)
                                    {
                                        start_time = end_time;
                                        previous_num_of_sentences_until_now = sentences_without_word_order.Count;
                                        label5.Invoke(new DisplayThingsDelegate(set_label5_text), "מספר התוצאות שנמצאו עד כה: " + previous_num_of_sentences_until_now.ToString());
                                    }
                                }
                                if ((sentences_without_word_order.Count > max_num_of_sentences) || vars.ct.IsCancellationRequested)
                                    break;
                                List<string> words_that_make_sentence = new List<string>();
                                int counter_length_sentence = 0;
                                foreach (int word_index in word_indexes_that_make_sentence)
                                {
                                    if (vars.respect_non_hebrew_symbols)
                                    {
                                        if (counter_length_sentence != 0)
                                            counter_length_sentence += 1; //the space before every word except the first word.
                                        counter_length_sentence += valid_words[word_index].word.Length;
                                    }
                                    else
                                        counter_length_sentence += HebrewWord.count_hebrew_letters(valid_words[word_index].word);
                                    words_that_make_sentence.Add(valid_words[word_index].word);
                                }
                                bool input_can_build_sentence = true;
                                if (vars.min_number_of_letters > counter_length_sentence)
                                {
                                    if (level_at_which_sentence_is_too_short < word_indexes_that_make_sentence.Count)
                                        level_at_which_sentence_is_too_short = word_indexes_that_make_sentence.Count;
                                }
                                else
                                {
                                    if (level_at_which_sentence_is_too_short >= word_indexes_that_make_sentence.Count)
                                        level_at_which_sentence_is_too_short = word_indexes_that_make_sentence.Count - 1;
                                }
                                if (words_that_make_sentence.Count > 1)
                                {
                                    string input_data_cpy = "";
                                    if (!vars.differentiate_ending_letters)
                                    {
                                        for (int char_input_data_counter = 0; char_input_data_counter < vars.input_data.Length; ++char_input_data_counter)
                                            input_data_cpy += HebrewWord.convert_to_not_ending_letter(vars.input_data[char_input_data_counter]);
                                    }
                                    else
                                        input_data_cpy = vars.input_data;
                                    bool is_first_word = true;
                                    foreach (string current_word in words_that_make_sentence)
                                    {
                                        if (!is_first_word)
                                        {
                                            if (vars.respect_non_hebrew_symbols)
                                            {
                                                if (input_data_cpy.Contains(' '))
                                                    input_data_cpy = input_data_cpy.Remove(input_data_cpy.IndexOf(' '), 1);
                                                else
                                                {
                                                    input_can_build_sentence = false;
                                                    break;
                                                }
                                            }
                                        }
                                        foreach (char ch_in_word in current_word)
                                        {
                                            char char_lower_in_word = ch_in_word;
                                            if (!vars.differentiate_ending_letters)
                                                char_lower_in_word = HebrewWord.convert_to_not_ending_letter(ch_in_word);
                                            if (vars.respect_non_hebrew_symbols || HebrewWord.HebrewLetters.Contains(ch_in_word))
                                            {
                                                if (input_data_cpy.Contains(char_lower_in_word))
                                                    input_data_cpy = input_data_cpy.Remove(input_data_cpy.IndexOf(char_lower_in_word), 1);
                                                else
                                                {
                                                    input_can_build_sentence = false;
                                                    break;
                                                }
                                            }
                                        }
                                        if (!input_can_build_sentence)
                                            break;
                                        is_first_word = false;
                                    }
                                }
                                if (input_can_build_sentence)
                                {
                                    if (level_at_which_sentence_is_too_short > word_indexes_that_make_sentence.Count)
                                        level_at_which_sentence_is_too_short = word_indexes_that_make_sentence.Count;
                                    word_indexes_that_make_sentence.Add(word_indexes_that_make_sentence[word_indexes_that_make_sentence.Count - 1]);
                                }
                                else
                                {
                                    if (word_indexes_that_make_sentence[word_indexes_that_make_sentence.Count - 1] >= (valid_words.Count - 1))
                                    {
                                        if (level_at_which_sentence_is_too_short < word_indexes_that_make_sentence.Count - 1)
                                        {
                                            Dictionary<string, int> current_sentence_unordered = new Dictionary<string, int>();
                                            for (int word_index = 0; word_index < (words_that_make_sentence.Count - 1); ++word_index)
                                            {
                                                string current_word = words_that_make_sentence[word_index];
                                                if (current_sentence_unordered.ContainsKey(current_word))
                                                    current_sentence_unordered[current_word] += 1;
                                                else
                                                    current_sentence_unordered.Add(current_word, 1);
                                            }
                                            if (current_sentence_unordered.Count > 0)
                                                sentences_without_word_order.Add(current_sentence_unordered);
                                        }
                                        do
                                        {
                                            word_indexes_that_make_sentence.RemoveAt(word_indexes_that_make_sentence.Count - 1);
                                            if (word_indexes_that_make_sentence.Count > 0)
                                            {
                                                word_indexes_that_make_sentence[word_indexes_that_make_sentence.Count - 1] += 1;
                                            }
                                            else
                                                break;
                                        } while (word_indexes_that_make_sentence[word_indexes_that_make_sentence.Count - 1] >= valid_words.Count);
                                        if (level_at_which_sentence_is_too_short >= word_indexes_that_make_sentence.Count)
                                            level_at_which_sentence_is_too_short = word_indexes_that_make_sentence.Count - 1;
                                    }
                                    else
                                    {
                                        word_indexes_that_make_sentence[word_indexes_that_make_sentence.Count - 1] += 1;
                                        if (level_at_which_sentence_is_too_short >= word_indexes_that_make_sentence.Count)
                                            level_at_which_sentence_is_too_short = word_indexes_that_make_sentence.Count - 1;
                                    }
                                }
                            }
                            if (sentences_without_word_order.Count < 1)
                                label3.Invoke(new DisplayThingsDelegate(display_error), "שגיאה: אין תוצאות.");
                            sentences_without_word_order.Sort((dic1, dic2) => HebrewWord.compare_dictionaries_based_on_occurence(dic2, dic1));
                            if (sentences_without_word_order.Count > max_num_of_sentences)
                            {
                                sentences_without_word_order.RemoveRange(max_num_of_sentences, sentences_without_word_order.Count - max_num_of_sentences);
                                label3.Invoke(new DisplayThingsDelegate(display_error), "שגיאה: יש יותר מדי תוצאות.");
                            }
                            for (int sentence_index = 0; sentence_index < sentences_without_word_order.Count; ++sentence_index)
                            {
                                text_of_answer += (sentence_index + 1).ToString() + ". ";
                                int counter_num_of_words_in_sentence = 0;
                                int num_of_words_in_sentence = sentences_without_word_order.Count;
                                foreach (KeyValuePair<string, int> current_word in sentences_without_word_order[sentence_index])
                                {
                                    for (int counter_repeat_word = 0; counter_repeat_word < current_word.Value; ++counter_repeat_word)
                                    {
                                        if (counter_num_of_words_in_sentence < num_of_words_in_sentence - 1)
                                            text_of_answer += current_word.Key + " ";
                                        else
                                            text_of_answer += current_word.Key;
                                        ++counter_num_of_words_in_sentence;
                                    }
                                }
                                if (sentence_index < sentences_without_word_order.Count - 1)
                                    text_of_answer += "\r\n";
                            }
                            button3.Invoke(new SetEnabled(set_button3_enabled), false);
                            label5.Invoke(new SetVisible(set_label5_visible), false);
                        }
                    }
                    textBox2.Invoke(new DisplayThingsDelegate(set_textBox2_text), text_of_answer);
                }
                else
                {
                    string appPath = Application.StartupPath;
                    label3.Invoke(new DisplayThingsDelegate(display_error), "שגיאה: הקובץ words.txt לא נמצא בתיקייה" + "\r\n" + appPath);
                    textBox2.Invoke(new DisplayThingsDelegate(set_textBox2_text), "תוצאה");
                }
                button1.Invoke(new SetEnabled(set_button1_enabled), true);
                cts.Dispose();
            });
            thread.IsBackground = true;
            thread.Start();
        }
        private delegate void DisplayThingsDelegate(string err_msg);
        private delegate void SetEnabled(bool enabled);
        private delegate void SetVisible(bool visible);
        private void display_error(string err_msg)
        {
            label3.Text = err_msg;
            label3.ForeColor = Color.Green;
            label3.BackColor = Color.Pink;
        }
        private void set_textBox2_text(string text)
        {
            textBox2.Text = text;
        }
        private void set_button1_enabled(bool enabled)
        {
            button1.Enabled = enabled;
        }
        private void set_button3_enabled(bool enabled)
        {
            button3.Enabled = enabled;
        }
        private void set_label5_visible(bool visible)
        {
            label5.Visible = visible;
        }
        private void set_label5_text(string text)
        {
            label5.Text = text;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            bool differentiate_ending_letters = checkBox1.Checked;
            bool respect_non_hebrew_symbols = checkBox3.Checked;
            bool allow_using_letters_more_than_once = checkBox2.Checked;
            bool include_results_with_non_letters = checkBox4.Checked;
            bool build_full_sentences = checkBox5.Checked;
            int min_number_of_letters = (int)numericUpDown1.Value;
            label3.Text = "כאן יופיעו השגיאות";
            label3.ForeColor = Color.Blue;
            label3.BackColor = Color.Yellow;
            label3.Refresh();
            string input_data = textBox1.Text;
            textBox2.Text = "מחשב...";
            textBox2.Refresh();
            cts = new CancellationTokenSource();
            ContainsEverythingNeeded stuff_to_send = new ContainsEverythingNeeded(min_number_of_letters, respect_non_hebrew_symbols, differentiate_ending_letters, allow_using_letters_more_than_once, build_full_sentences, include_results_with_non_letters, input_data, cts.Token);
            start_thread(stuff_to_send);
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
            checkBox5.Enabled = true;
            numericUpDown1.Enabled = true;
            checkBox6.Enabled = true;
            label4.Enabled = true;
            checkBox6.Checked = true;
            checkBox5.Checked = true;
        }
        void update_numericUpDown1_based_on_textBox1()
        {
            int text_box_text_length;
            if (checkBox3.Checked)
                text_box_text_length = textBox1.Text.Length;
            else
                text_box_text_length = HebrewWord.count_hebrew_letters(textBox1.Text);
            if (text_box_text_length < 1)
                numericUpDown1.Value = 1;
            else
                numericUpDown1.Value = text_box_text_length;
        }
        static bool checkBox2_was_previously_checked = false;
        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox6.Checked)
            {
                checkBox2.Enabled = false;
                if (checkBox2.Checked)
                {
                    checkBox2.Checked = false;
                    checkBox2_was_previously_checked = true;
                }
                numericUpDown1.Enabled = false;
                tracking_input_length = true;
                textBox1.Refresh();
                update_numericUpDown1_based_on_textBox1();
            }
            else
            {
                tracking_input_length = false;
                numericUpDown1.Enabled = true;
                numericUpDown1.Value = 1;
                if (checkBox2_was_previously_checked)
                    checkBox2.Checked = true;
                checkBox2_was_previously_checked = false;
                checkBox2.Enabled = true;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (tracking_input_length)
                update_numericUpDown1_based_on_textBox1();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (tracking_input_length)
                update_numericUpDown1_based_on_textBox1();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            cts.Cancel();
        }
    }
}
