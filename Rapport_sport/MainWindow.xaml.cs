﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace Rapport_sport
{
    public partial class MainWindow : Window
    {
        private List<TrainingSession> sessions;

        public MainWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            string csvFilePath = @"C:\Chris.csv";
            if (File.Exists(csvFilePath))
            {
                var lines = File.ReadAllLines(csvFilePath).Skip(1); // Skip header
                sessions = new List<TrainingSession>();

                foreach (var line in lines)
                {
                    try
                    {
                        var data = line.Split(',');

                        var session = new TrainingSession
                        {
                            Date = DateTime.Parse(data[0]),
                            WorkoutName = data[1],
                            Exercise = data[2],
                            Set = int.Parse(data[3]),
                            Weight = string.IsNullOrEmpty(data[4]) ? (double?)null : double.Parse(data[4]),
                            Reps = string.IsNullOrEmpty(data[5]) ? (int?)null : int.Parse(data[5]),
                            Distance = string.IsNullOrEmpty(data[6]) ? null : data[6],
                            Duration = string.IsNullOrEmpty(data[7]) ? null : data[7],
                            MeasurementUnit = data[8],
                            Notes = data[9]
                        };

                        sessions.Add(session);
                    }
                    catch (FormatException ex)
                    {
                        Console.WriteLine($"Error parsing line: {line}. Exception: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unexpected error parsing line: {line}. Exception: {ex.Message}");
                    }
                }
            }
        }

        private void TrainingCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrainingCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = TrainingCalendar.SelectedDate.Value;
                LoadSessionsForDate(selectedDate);
            }
        }

        private void LoadSessionsForDate(DateTime date)
        {
            var sessionsForDate = sessions
                .Where(s => s.Date.Date == date.Date)
                .GroupBy(s => s.Exercise.Trim())
                .ToList();

            var displayList = new List<UIElement>();

            foreach (var group in sessionsForDate)
            {
                var previousSession = sessions
                    .Where(ps => ps.Exercise.Trim() == group.Key && ps.Date < date)
                    .OrderByDescending(ps => ps.Date)
                    .FirstOrDefault();

                var headerTextBlock = new TextBlock
                {
                    Text = $"{group.Key}",
                    FontWeight = FontWeights.Bold,
                    Background = new SolidColorBrush(Colors.LightGray),
                    Padding = new Thickness(5),
                    Margin = new Thickness(0, 5, 0, 5)
                };

                if (previousSession != null)
                {
                    headerTextBlock.Text += $" (Précédent : {previousSession.Date.ToString("dd/MM/yyyy")})";
                }

                displayList.Add(headerTextBlock);

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition());

                int rowIndex = 0;

                foreach (var session in group)
                {
                    grid.RowDefinitions.Add(new RowDefinition());

                    var sessionTextBlock = new TextBlock
                    {
                        Margin = new Thickness(10, 0, 0, 0)
                    };

                    sessionTextBlock.Inlines.Add(new Run($"Set: {session.Set}, Poids: "));
                    sessionTextBlock.Inlines.Add(new Run(session.Weight.HasValue ? session.Weight.Value.ToString() : "N/A")
                    {
                        FontWeight = FontWeights.Bold
                    });
                    sessionTextBlock.Inlines.Add(new Run(" kg, Reps: "));
                    sessionTextBlock.Inlines.Add(new Run(session.Reps.HasValue ? session.Reps.Value.ToString() : "N/A")
                    {
                        FontWeight = FontWeights.Bold
                    });

                    if (session.Weight.HasValue && session.Reps.HasValue)
                    {
                        double oneRm = session.Weight.Value * (1 + 0.025 * session.Reps.Value);
                        int roundedOneRm = (int)Math.Round(oneRm);
                        var oneRmRun = new Run($" 1RM: {roundedOneRm}")
                        {
                            Foreground = new SolidColorBrush(Colors.Red),
                            FontWeight = FontWeights.Bold
                        };
                        sessionTextBlock.Inlines.Add(oneRmRun);
                    }

                    Grid.SetColumn(sessionTextBlock, 0);
                    Grid.SetRow(sessionTextBlock, rowIndex);
                    grid.Children.Add(sessionTextBlock);

                    if (previousSession != null)
                    {
                        var previousSessionTextBlock = new TextBlock
                        {
                            Margin = new Thickness(10, 0, 0, 0),
                            Foreground = new SolidColorBrush(Colors.Gray)
                        };

                        previousSessionTextBlock.Inlines.Add(new Run($"Set: {previousSession.Set}, Poids: "));
                        previousSessionTextBlock.Inlines.Add(new Run(previousSession.Weight.HasValue ? previousSession.Weight.Value.ToString() : "N/A")
                        {
                            FontWeight = FontWeights.Bold
                        });
                        previousSessionTextBlock.Inlines.Add(new Run(" kg, Reps: "));
                        previousSessionTextBlock.Inlines.Add(new Run(previousSession.Reps.HasValue ? previousSession.Reps.Value.ToString() : "N/A")
                        {
                            FontWeight = FontWeights.Bold
                        });

                        if (previousSession.Weight.HasValue && previousSession.Reps.HasValue)
                        {
                            double previousOneRm = previousSession.Weight.Value * (1 + 0.025 * previousSession.Reps.Value);
                            int previousRoundedOneRm = (int)Math.Round(previousOneRm);
                            previousSessionTextBlock.Inlines.Add(new Run($" 1RM: {previousRoundedOneRm}")
                            {
                                Foreground = new SolidColorBrush(Colors.Red),
                                FontWeight = FontWeights.Bold
                            });
                        }

                        Grid.SetColumn(previousSessionTextBlock, 1);
                        Grid.SetRow(previousSessionTextBlock, rowIndex);
                        grid.Children.Add(previousSessionTextBlock);
                    }

                    rowIndex++;
                }

                displayList.Add(grid);
            }

            SessionList.ItemsSource = displayList;
        }



        private void TrainingCalendar_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            MarkTrainingDays();
        }

        private void TrainingCalendar_Loaded(object sender, RoutedEventArgs e)
        {
            MarkTrainingDays();
        }

        private void MarkTrainingDays()
        {
            if (TrainingCalendar == null || sessions == null) return;

            var calendarDays = GetCalendarDayButtons(TrainingCalendar);

            foreach (var button in calendarDays)
            {
                DateTime date;
                if (DateTime.TryParse(button.DataContext.ToString(), out date))
                {
                    if (sessions.Any(s => s.Date.Date == date.Date))
                    {
                        button.Background = Brushes.LightGreen;
                    }
                    else
                    {
                        button.ClearValue(Button.BackgroundProperty);
                    }
                }
            }
        }

        private IEnumerable<CalendarDayButton> GetCalendarDayButtons(Calendar calendar)
        {
            if (calendar == null) yield break;

            var monthControl = FindVisualChild<CalendarItem>(calendar);
            if (monthControl == null) yield break;

            foreach (var button in monthControl.FindChildren<CalendarDayButton>())
            {
                yield return button;
            }
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; VisualTreeHelper.GetChildrenCount(parent) > i; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                {
                    return tChild;
                }
                var childOfChild = FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }
            return null;
        }

        private List<TrainingSession> GetBestPerformances()
        {
            return sessions
                .GroupBy(s => s.Exercise)
                .Select(g => g.OrderByDescending(s => s.Weight).First())
                .ToList();
        }

        private void ShowBestPerformances(object sender, RoutedEventArgs e)
        {
            var bestPerformances = GetBestPerformances();
            var bestPerformancesWindow = new BestPerformancesWindow(bestPerformances);
            bestPerformancesWindow.Show();
        }
    }

    public static class VisualTreeHelpers
    {
        public static IEnumerable<T> FindChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;

            for (int i = 0; VisualTreeHelper.GetChildrenCount(parent) > i; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                {
                    yield return tChild;
                }

                foreach (var childOfChild in FindChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }
    }

    public class TrainingSession
    {
        public DateTime Date { get; set; }
        public string WorkoutName { get; set; }
        public string Exercise { get; set; }
        public int Set { get; set; }
        public double? Weight { get; set; }
        public int? Reps { get; set; }
        public string Distance { get; set; }
        public string Duration { get; set; }
        public string MeasurementUnit { get; set; }
        public string Notes { get; set; }

        public int OneRM
        {
            get
            {
                if (Weight.HasValue && Reps.HasValue)
                {
                    double oneRm = Weight.Value * (1 + 0.025 * Reps.Value);
                    return (int)Math.Round(oneRm);
                }
                return 0;
            }
        }

        public override string ToString()
        {
            var details = new List<string>();
            if (Weight.HasValue)
            {
                details.Add($"Poids: {Weight.Value} {MeasurementUnit}");
            }
            if (Reps.HasValue)
            {
                details.Add($"Reps: {Reps.Value}");
            }
            if (!string.IsNullOrEmpty(Distance))
            {
                details.Add($"Distance: {Distance}");
            }
            if (!string.IsNullOrEmpty(Duration))
            {
                details.Add($"Duration: {Duration}");
            }
            if (!string.IsNullOrEmpty(Notes))
            {
                details.Add($"Notes: {Notes}");
            }

            return $"Set: {Set}, {string.Join(", ", details)}";
        }
    }
}
