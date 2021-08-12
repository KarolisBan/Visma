using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Visma.Entities;

namespace Visma
{
    public class Program
    {
        static string directory = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName + @"\Data\";
        static string fileName = "books.json";
        static string path = directory + fileName;
        static bool appRunning = true;
        static List<Book> books;

        static List<PropertyInfo> propertiesWithoutLog = new List<PropertyInfo>();
        static List<PropertyInfo> propertiesForListing = new List<PropertyInfo>();


        static int maxTakeOuts = 3; // max number of books taken out at the same time by the same person
        static int maxMonthsTakenOut = 2; // maximum take out month deadline 

        public static void Main(string[] args)
        {
            LoadProperties();
            books = LoadBooks();
            Console.WriteLine("Type \"help\" to view command list.");
            while (appRunning)
            {
                Console.Write("Input command: ");
                string consoleInput = Console.ReadLine().ToLower();
                switch (consoleInput)
                {
                    case "add":
                        Book newBook = new Book();
                        foreach (PropertyInfo prop in propertiesWithoutLog)
                        {
                            if (prop.PropertyType.Name == "String")
                            {
                                Console.Write("Enter " + prop.Name + ": ");
                                consoleInput = Console.ReadLine();
                                prop.SetValue(newBook, consoleInput == string.Empty ? "-" : consoleInput);
                            }
                            else if (prop.PropertyType.Name == "Double")
                            {
                                while (true)
                                {
                                    Console.Write("Enter " + prop.Name + ": ");
                                    double inputISBN;
                                    if (double.TryParse(Console.ReadLine(), out inputISBN) && inputISBN.ToString().Length < 14 && inputISBN.ToString().Length > 0)
                                    {
                                        prop.SetValue(newBook, inputISBN);
                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid ISBN (13 digit number)");
                                    }
                                }
                                if (prop.Name == "ISBN")
                                {
                                    if (books.Where(e => e.ISBN == Convert.ToDouble(prop.GetValue(newBook))).Any())
                                    {
                                        Console.WriteLine("ISBN number conflict.");
                                        break;
                                    }
                                }
                            }
                            else if (prop.PropertyType.Name == "Nullable`1")
                            {
                                while (true)
                                {
                                    Console.Write("Enter Publication date (format: YYYY-MM-DD): ");
                                    DateTime inputDate;
                                    if (DateTime.TryParse(Console.ReadLine(), out inputDate) && inputDate < DateTime.Today.AddDays(1))
                                    {
                                        prop.SetValue(newBook, inputDate);
                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid date");

                                    }
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        if (newBook.Publication != null)
                        {
                            Console.WriteLine();
                            DisplayBook(newBook, true, propertiesWithoutLog);
                            while (true)
                            {
                                Console.Write("Save this book? Y/N  ");
                                consoleInput = Console.ReadLine().ToLower();
                                if (consoleInput == "y" || consoleInput == "yes")
                                {
                                    InsertBook(newBook);
                                    Console.WriteLine("New entry saved");
                                    break;
                                }
                                else if (consoleInput == "n" || consoleInput == "no")
                                {
                                    Console.WriteLine("Saving canceled");
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Invalid command");
                                }
                            }
                            Console.WriteLine();
                        }
                        break;

                    case "take":
                        Console.Write("Enter your name: ");
                        string userNameTake = Console.ReadLine();
                        List<Book> booksTakenByUserTake = books.Where(e => e.LogTakenBy == userNameTake).ToList();
                        if (booksTakenByUserTake.Count() >= maxTakeOuts)
                        {
                            Console.WriteLine("You cannot take out more books");
                        }
                        else
                        {
                            List<Book> availableBooksTake = books.Where(e => e.LogTaken == false).ToList();
                            int indexTake = 1;
                            foreach (var book in availableBooksTake)
                            {
                                Console.Write("[" + indexTake++ + "] ");
                                DisplayBook(book, true, propertiesWithoutLog);
                            }
                            Console.Write("Select which book to take out? ");
                            consoleInput = Console.ReadLine();
                            int selectedBookNumberTake;
                            Book selectedBookTake;
                            if (int.TryParse(consoleInput, out selectedBookNumberTake) && selectedBookNumberTake <= books.Count() && selectedBookNumberTake > 0)
                            {
                                DateTime returnDate;
                                while (true)
                                {
                                    Console.Write("Enter return date (format: YYYY-MM-DD): ");
                                    if (DateTime.TryParse(Console.ReadLine(), out returnDate) && returnDate <= DateTime.Today.AddMonths(maxMonthsTakenOut) && returnDate > DateTime.Today)
                                    {
                                        selectedBookTake = availableBooksTake[selectedBookNumberTake - 1];
                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid date (maximum take out time is " + maxMonthsTakenOut + " months (" + DateTime.Today.AddMonths(maxMonthsTakenOut).ToShortDateString() + ")) \n");
                                    }
                                }
                                Console.WriteLine();
                                DisplayBook(selectedBookTake, true, propertiesWithoutLog);
                                while (true)
                                {
                                    Console.Write("Take out this book untill " + returnDate.ToShortDateString() + "?  Y/N  ");
                                    consoleInput = Console.ReadLine().ToLower();
                                    if (consoleInput == "y" || consoleInput == "yes")
                                    {
                                        Book result = books.Where(e =>
                                        {
                                            if (e.ISBN == selectedBookTake.ISBN)
                                            {
                                                e.LogTaken = true;
                                                e.LogTakenBy = userNameTake;
                                                e.LogTakeOutDate = DateTime.Now;
                                                e.LogPlannedReturnDate = returnDate;
                                                return true;
                                            }
                                            return false;
                                        }).First();
                                        UpdateBooks();
                                        Console.WriteLine("Book taken out");
                                        break;
                                    }
                                    else if (consoleInput == "n" || consoleInput == "no")
                                    {
                                        Console.WriteLine("Book take out canceled");
                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid command");
                                    }
                                }
                                Console.WriteLine();
                            }
                            else
                            {
                                Console.WriteLine("Book not found");
                            }
                        }
                        break;

                    case "return":
                        Console.Write("Enter your name: ");
                        string userNameReturn = Console.ReadLine();
                        List<Book> booksTakenByUserReturn = books.Where(e => e.LogTakenBy == userNameReturn).ToList();
                        if (booksTakenByUserReturn.Count() == 0)
                        {
                            Console.WriteLine("You don't have any books to return \n");
                        }
                        else
                        {
                            int index = 1;
                            foreach (var book in booksTakenByUserReturn)
                            {
                                Console.Write("[" + index++ + "] ");
                                DisplayBook(book, true, propertiesWithoutLog);
                            }
                            Console.Write("Select which book to return? ");
                            consoleInput = Console.ReadLine();
                            int selectedBookNumberReturn;
                            Book selectedBookReturn;
                            if (int.TryParse(consoleInput, out selectedBookNumberReturn) && selectedBookNumberReturn <= books.Count() && selectedBookNumberReturn > 0)
                            {
                                selectedBookReturn = booksTakenByUserReturn[selectedBookNumberReturn - 1];
                                Book result = books.Where(e =>
                                {
                                    if (e.ISBN == selectedBookReturn.ISBN)
                                    {
                                        if (e.LogPlannedReturnDate < DateTime.Today)
                                            Console.WriteLine("Book was returned late");
                                        e.LogTaken = false;
                                        e.LogTakenBy = string.Empty;
                                        e.LogTakeOutDate = null;
                                        e.LogPlannedReturnDate = null;
                                        return true;
                                    }
                                    return false;
                                }).First();
                                UpdateBooks();
                                Console.WriteLine("Book returned");
                            }
                            else
                            {
                                Console.WriteLine("Book not found");
                            }
                            Console.WriteLine();
                        }
                        break;

                    case "list":
                        int indexList = 1;
                        foreach (PropertyInfo prop in propertiesForListing)
                        {
                            if (prop.Name == nameof(Book.LogTaken))
                            {
                                Console.Write("[" + indexList++ + "]Availability ");
                            }
                            else
                            {
                                Console.Write("[" + indexList++ + "]" + prop.Name + " ");
                            }
                        }
                        Console.WriteLine();
                        Console.Write("Filter list by which property (leave empty for default filtering)? ");
                        int filteringIndex;
                        List<Book> filteredBooks = books;
                        if (int.TryParse(Console.ReadLine(), out filteringIndex) && filteringIndex <= propertiesForListing.Count() && filteringIndex > 0)
                        {
                            filteredBooks = filteredBooks.OrderBy(e => propertiesForListing[filteringIndex - 1].GetValue(e)).ToList();
                        }
                        foreach (var book in filteredBooks)
                        {
                            DisplayBook(book, true, propertiesForListing);
                        }
                        Console.WriteLine();
                        break;

                    case "remove":
                        int indexRemove = 1;
                        foreach (var book in books)
                        {
                            Console.Write("[" + indexRemove++ + "] ");
                            DisplayBook(book, true, propertiesWithoutLog);
                        }
                        Console.Write("Select which book to remove? ");
                        consoleInput = Console.ReadLine();
                        int selectedBookNumberRemove;
                        if (int.TryParse(consoleInput, out selectedBookNumberRemove) && selectedBookNumberRemove <= books.Count() && selectedBookNumberRemove > 0)
                        {
                            books.RemoveAt(selectedBookNumberRemove - 1);
                            UpdateBooks();
                            Console.WriteLine("Book removed");
                        }
                        else
                        {
                            Console.WriteLine("Book not found");
                        }
                        Console.WriteLine();
                        break;

                    case "help":
                        Console.WriteLine($"Available commands:  \"add\", \"take\", \"return\", \"list\", \"remove\", \"exit\" ");
                        break;

                    case "exit":
                        appRunning = false;
                        break;

                    default:
                        Console.WriteLine("Unrecognized command type \"help\" for command list ");
                        break;
                }
            }
        }

        public static void LoadProperties()
        {
            foreach (PropertyInfo prop in typeof(Book).GetProperties())
            {
                if (!prop.Name.StartsWith("Log"))
                {
                    propertiesWithoutLog.Add(prop);
                }

                if (!prop.Name.StartsWith("Log") && prop.Name != nameof(Book.Publication))
                {
                    propertiesForListing.Add(prop);
                }
                else if (prop.Name == nameof(Book.LogTaken))
                {
                    propertiesForListing.Add(prop);
                }
            }
        }

        public static List<Book> LoadBooks()
        {
            IEnumerable<Book> loadedBooks = new List<Book>();
            //Console.WriteLine("The current directory is {0}", path);
            //Console.WriteLine(File.Exists(path) ? "File exists." : "File does not exist.");
            if (File.Exists(path))
            {
                loadedBooks = ReadFromFile();
            }
            else
            {
                loadedBooks = CreateNewFile();
            }
            return loadedBooks.ToList();
        }
        public static void InsertBook(Book bookToInsert)
        {
            books.Add(bookToInsert);
            string json = ConvertToJson(books);
            WriteToFile(json);
        }
        public static void UpdateBooks()
        {
            string json = ConvertToJson(books);
            WriteToFile(json);
        }
        public static void WriteToFile(string json)
        {
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(directory, fileName)))
            {
                outputFile.WriteLine(json);
            }
        }
        public static IEnumerable<Book> ReadFromFile()
        {
            IEnumerable<Book> readBooks;
            StreamReader sw = new StreamReader(path);
            string jsonString = sw.ReadToEnd();
            readBooks = ConvertFromJson(jsonString);
            if (readBooks == null)
            {
                readBooks = new List<Book>();
            }
            sw.Close();
            return readBooks;
        }
        public static IEnumerable<Book> CreateNewFile()
        {
            File.Create(path);
            return new List<Book>();
        }
        public static void DisplayBook(Book bookToDisplay, bool inline, List<PropertyInfo> properties)
        {
            foreach (PropertyInfo prop in properties)
            {
                if (prop.PropertyType.Name != "Nullable`1")
                {
                    if (prop.Name == nameof(Book.LogTaken))
                    {
                        Console.Write("Available: " + !(bool)prop.GetValue(bookToDisplay));

                    }
                    else
                    {
                        Console.Write(prop.Name + ": " + prop.GetValue(bookToDisplay));
                    }

                }
                else if (prop.PropertyType.Name == "Nullable`1")
                {
                    DateTime date = (DateTime)prop.GetValue(bookToDisplay);
                    Console.Write(prop.Name + ": " + date.ToShortDateString() + "  ");
                }
                if (inline)
                    Console.Write("  ");
                else
                    Console.WriteLine();
            }
            if (inline)
            {
                Console.WriteLine();
            }
        }
        public static string ConvertToJson(List<Book> concent)
        {
            var serialized = JsonConvert.SerializeObject(concent);
            return serialized;
        }
        public static IEnumerable<Book> ConvertFromJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<IEnumerable<Book>>(json);
            }
            catch (Exception e)
            {
                return null;
            }
        }

    }
}
