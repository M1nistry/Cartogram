using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Cartogram.SQL;

namespace Cartogram
{
    public partial class Details : Window
    {
        private readonly MainWindow _main;
        private Map MapDetails { get; }
        public Details(Map selectedMap)
        {
            InitializeComponent();
            _main = MainWindow.GetSingleton();
            Icon = _main.Icon;
            if (selectedMap != null)
            {
                MapDetails = selectedMap;
                PopulateDetails();
            }
        }

        private void PopulateDetails()
        {
            LabelMapTitle.Text = $"{MapDetails.Rarity} {MapDetails.Name} Map";
            LabelMapLevel.Text = $"Level {MapDetails.Level}";
            LabelQuantity.Text = $"Quantity: {MapDetails.Quantity}%";
            LabelQuality.Text = $"Quality: {MapDetails.Quality}%";
            var affixCount = 1;
            foreach (var newAffix in MapDetails.Affixes.Select(affix => new TextBlock
            {
                Name = $"TextAffix{affixCount}",
                Text = affix,
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 10.667
            }))
            {
                StackAffixes.Children.Add(newAffix);
                affixCount++;
                
            }
            LabelLeague.Text = $"League: {MapDetails.League}";
            LabelCharacter.Text = $"Character: {MapDetails.Character}";
            LabelDate.Text = MapDetails.StartAt.ToString(CultureInfo.CurrentCulture);
            var mapDuration = MapDetails.FinishAt - MapDetails.StartAt;
            LabelDuration.Text = $"Duration: {mapDuration}";
            //var expGained = MapDetails.ExpAfter.CurrentExperience - MapDetails.ExpBefore.CurrentExperience;
            //var expGoal = Sqlite.ExperienceGoal(MapDetails.ExpBefore.Level);
            //var percentGained = (float) expGained/expGoal;
            //LabelExperience.Text = $"Experience Gained: {expGained:#,##0} ({percentGained:P2})";
            if (MapDetails.Notes != string.Empty)
            {
                TextBoxNotes.Text = MapDetails.Notes;
                ExpanderNotes.IsExpanded = true;
            }
        }
    }

    public class MultiplyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return values.OfType<double>().Aggregate(1.0, (current, t) => current*t);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new Exception("Not implemented");
        }
    }
}
