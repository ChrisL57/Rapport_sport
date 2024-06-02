﻿using System.Collections.Generic;
using System.Windows;

namespace Rapport_sport
{
    public partial class BestPerformancesWindow : Window
    {
        public BestPerformancesWindow(List<TrainingSession> bestPerformances)
        {
            InitializeComponent();
            BestPerformancesListView.ItemsSource = bestPerformances;
        }
    }
}
