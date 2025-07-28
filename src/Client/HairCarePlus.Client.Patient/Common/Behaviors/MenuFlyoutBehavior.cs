using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Linq;

namespace HairCarePlus.Client.Patient.Common.Behaviors
{
    public class MenuFlyoutBehavior : Behavior<Button>
    {
        private Button _button;
        private ObservableCollection<MenuFlyoutItem> _menuItems;

        public MenuFlyoutBehavior()
        {
            _menuItems = new ObservableCollection<MenuFlyoutItem>();
        }

        public ObservableCollection<MenuFlyoutItem> MenuItems
        {
            get => _menuItems;
        }

        protected override void OnAttachedTo(Button button)
        {
            base.OnAttachedTo(button);
            _button = button;
            _button.Clicked += OnButtonClicked;
        }

        protected override void OnDetachingFrom(Button button)
        {
            base.OnDetachingFrom(button);
            if (_button != null)
            {
                _button.Clicked -= OnButtonClicked;
            }
            _button = null;
        }

        private async void OnButtonClicked(object sender, EventArgs e)
        {
            if (_button == null || !MenuItems.Any()) return;

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;
            var result = await page.DisplayActionSheet(
                "Choose Action",
                "Cancel",
                null,
                MenuItems.Select(item => item.Text).ToArray());

            if (result == null || result == "Cancel") return;

            var selectedItem = MenuItems.FirstOrDefault(item => item.Text == result);
            if (selectedItem?.Command != null)
            {
                selectedItem.Command.Execute(selectedItem.CommandParameter);
            }
        }

        public void AddMenuItem(MenuFlyoutItem item)
        {
            MenuItems.Add(item);
        }
    }
} 