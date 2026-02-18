using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using UP.Helpers;
using UP.Models;
using UP.Services;

namespace UP.Pages
{
    public partial class Receipts : Page
    {
        private ObservableCollection<IngredientDto> _products;
        private ObservableCollection<DailyMenuViewModel> _weeklyMenu;
        private ObservableCollection<AvailableMenu> _availableMenus;

        private ObservableCollection<ShoppingListItemDto> _shoppingList;

        public Receipts()
        {
            InitializeComponent();
            InitializeData();
            LoadUserInfo();
            LoadFridgeIngridients();
            IsAdmin();
        }

        private void IsAdmin()
        {
            if (AppData.CurrentUser.IsAdmin)
            {
                usContrBut.Visibility = Visibility.Visible;
            }
        }

        private async void LoadFridgeIngridients()
        {
            var products = await AppData.ApiService.UpdateFridgeAsync(AppData.CurrentUser.Id);
            if (products != null)
            {
                _products = new ObservableCollection<IngredientDto>(products);
                ProductsListView.ItemsSource = _products;
            }
        }

        private void InitializeData()
        {
            _products = new ObservableCollection<IngredientDto>();

            _weeklyMenu = AppData.WeeklyMenu;
            _shoppingList = AppData.ShoppingList.Count > 0
                ? new ObservableCollection<ShoppingListItemDto>() 
                : new ObservableCollection<ShoppingListItemDto>();

            _availableMenus = new ObservableCollection<AvailableMenu>();

            if (AppData.Products.Count > 0)
            {
                _products = AppData.Products;
            }

            ProductsListView.ItemsSource = _products;
            WeeklyMenuItemsControl.ItemsSource = _weeklyMenu;
            AvailableMenusListView.ItemsSource = _availableMenus;
            ShoppingListListView.ItemsSource = _shoppingList;

            LoadAvailableMenusOnInit();
        }

        private async void LoadAvailableMenusOnInit()
        {
            await LoadAvailableMenus();
        }

        private void LoadUserInfo()
        {
            if (AppData.CurrentUser != null)
            {
                UserInfoText.Text = $"Пользователь: {AppData.CurrentUser.Username}";
            }
        }

        private async void SelectionChangedMenu(object sender, SelectionChangedEventArgs e)
        {
            if (AvailableMenusListView.SelectedItem is AvailableMenu selectedMenu)
            {
                await DisplayMenuDetails(selectedMenu);
            }
        }

        private async Task DisplayMenuDetails(AvailableMenu selectedMenu)
        {
            try
            {
                var fullMenuDto = await AppData.ApiService.GetMenuDetailsAsync(selectedMenu.Id);

                if (fullMenuDto == null)
                {
                    ToastContainer.ShowToast("Не удалось загрузить детали меню", Elements.ToastType.Error);
                    return;
                }

                CurrentMenuTitle.Text = fullMenuDto.Name;

                _weeklyMenu.Clear();

                if (fullMenuDto.Days != null)
                {
                    foreach (var day in fullMenuDto.Days)
                    {
                        foreach (var meal in day.Meals)
                        {
                            _weeklyMenu.Add(new DailyMenuViewModel
                            {
                                Day = day.Date,
                                Meal = meal.MealType,
                                Description = meal.RecipeTitle,
                                RecipeId = meal.RecipeId
                            });
                        }
                    }
                }

                await GenerateShoppingListForMenu(selectedMenu.Id);
            }
            catch (Exception ex)
            {
                ToastContainer.ShowToast("Не удалось загрузить меню: " + ex.Message, Elements.ToastType.Error);
            }
        }

        private async Task GenerateShoppingListForMenu(int menuId)
        {
            try
            {
                ShoppingListInfo.Text = "Генерация списка...";
                var success = await AppData.ApiService.GenerateShoppingListAsync(menuId);

                if (success)
                {
                    var shopingList = await AppData.ApiService.GetCurrentShoppingListAsync();

                    await AppData.ApiService.SetIngredientsPrice(shopingList);
                    
                    await AppData.LoadShoppingList();

                    ShoppingListInfo.Text = $"Загружено {_shoppingList.Count} товаров";
                    ToastContainer.ShowToast("Список покупок готов!", Elements.ToastType.Success);
                }
                else
                {
                    ShoppingListInfo.Text = "Ошибка генерации";
                    ToastContainer.ShowToast("Не удалось сгенерировать список покупок", Elements.ToastType.Error);
                }
            }
            catch (Exception ex)
            {
                ShoppingListInfo.Text = "Ошибка при генерации";
                ToastContainer.ShowToast("Ошибка: " + ex.Message, Elements.ToastType.Error);
                Console.WriteLine($"GenerateShoppingList error: {ex}");
            }
        }

        private void OpenRecipe_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int? recipeId = null;

                if (sender is Button button)
                {
                    if (button.Tag is int id && id > 0)
                    {
                        recipeId = id;
                    }
                    else if (button.DataContext is DailyMenuViewModel dailyMenu && dailyMenu.RecipeId > 0)
                    {
                        recipeId = dailyMenu.RecipeId;
                    }
                }

                if (recipeId.HasValue)
                {
                    MainWindow.mainWindow.OpenPages(new RecipeDetailsPage(recipeId.Value));
                    return;
                }
                ToastContainer.ShowToast("Не удалось загрузить рецепт", Elements.ToastType.Error);
            }
            catch (Exception ex)
            {
                ToastContainer.ShowToast($"Ошибка загрузки рецепта: {ex.Message}", Elements.ToastType.Error);
            }
        }

        private async Task LoadAvailableMenus()
        {
            try
            {
                _availableMenus.Clear();
                var menusFromApi = await AppData.ApiService.GetUserMenusAsync(AppData.CurrentUser.Id);

                foreach (var m in menusFromApi)
                {
                    _availableMenus.Add(m);
                }
            }
            catch (Exception ex)
            {
                ToastContainer.ShowToast("Не удалось загрузить список меню: " + ex.Message, Elements.ToastType.Error);
            }
        }

        private async void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            var name = NewProductTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) return;

            var id = await AppData.ApiService.FindIngredientIdByNameAsync(name);
            if (id == null) id = await AppData.ApiService.CreateIngredientByNameAsync(name);

            if (id != null)
            {
                var ingredientDto = new IngredientDto
                {
                    Id = id.Value,
                    Name = name,
                    Unit = "шт",
                    Category = "Разное"
                };

                if (await AppData.ApiService.AddFridgeItem(AppData.CurrentUser.Id, ingredientDto)) {
                    List<string> productNames = new List<string>();
                    foreach (var item in _products){
                        productNames.Add(item.Name);
                    }
                    
                    if (!productNames.Contains(ingredientDto.Name))
                        _products.Add(ingredientDto);
                }
            }
            NewProductTextBox.Clear();
        }

        private async void UsersControl_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.mainWindow.OpenPages(new Pages.Admin.Users());
        }

        private async Task GenerateMenu()
        {
            try
            {
                if (AppData.CurrentUser == null)
                {
                    ToastContainer.ShowToast("Пользователь не авторизован", Elements.ToastType.Error);
                    return;
                }

                var productsList = _products.ToList();
                if (!productsList.Any())
                {
                    ToastContainer.ShowToast("В холодильнике нет продуктов", Elements.ToastType.Info);
                    return;
                }


                var menu = await AppData.ApiService.GenerateMenuFromGigaChatAsync(AppData.CurrentUser.Id, productsList);

                if (menu == null)
                {
                    ToastContainer.ShowToast("Не удалось сгенерировать меню", Elements.ToastType.Error);
                    return;
                }

                await LoadAvailableMenus();

                if (_availableMenus.Count > 0)
                {
                    var lastMenu = _availableMenus.OrderByDescending(m => m.Id).FirstOrDefault();
                    if (lastMenu != null)
                    {
                        AvailableMenusListView.SelectedItem = lastMenu;
                        await DisplayMenuDetails(lastMenu);
                    }
                }

                ToastContainer.ShowToast("Холодильник обновлен и меню сгенерировано!", Elements.ToastType.Success);
            }
            catch (Exception ex)
            {
                ToastContainer.ShowToast($"Ошибка: {ex.Message}", Elements.ToastType.Error);
            }
        }

        private async void RemoveProduct_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is IngredientDto product)
            {
                if (await AppData.ApiService.RemoveFridgeAsync(AppData.CurrentUser.Id, product.Id))
                    _products.Remove(product);
            }

        }

        private async void GenerateShoppingList_Click(object sender, RoutedEventArgs e)
        {
            if (!(AvailableMenusListView.SelectedItem is AvailableMenu selectedMenu))
            {
                ToastContainer.ShowToast("Сначала выберите меню из списка", Elements.ToastType.Info);
                return;
            }

            await GenerateShoppingListForMenu(selectedMenu.Id);
            ToastContainer.ShowToast("Список покупок обновлен!", Elements.ToastType.Success);
        }

        private void ExportShoppingList_Click(object sender, RoutedEventArgs e)
        {
            if (_shoppingList == null || _shoppingList.Count == 0)
            {
                ToastContainer.ShowToast("Список покупок пуст.", Elements.ToastType.Info);
                return;
            }

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt",
                FileName = "Список_покупок.txt"
            };

            if (saveDialog.ShowDialog() == true)
            {
                var lines = new List<string> { "Список покупок", "==================" };
                foreach (var item in _shoppingList)
                {
                    lines.Add($"[ ] {item.Name} ({item.Unit})");
                }
                System.IO.File.WriteAllLines(saveDialog.FileName, lines);
                ToastContainer.ShowToast("Файл сохранен", Elements.ToastType.Success);
            }
        }

        private void ClearShoppingList_Click(object sender, RoutedEventArgs e)
        {
            _shoppingList.Clear();
            ShoppingListInfo.Text = "Список покупок очищен";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            AppData.Logout();
            MainWindow.mainWindow.OpenPages(new LogInPage());
        }

        private async void RefreshMenus_Click(object sender, RoutedEventArgs e)
        {
            await LoadAvailableMenus();
            ToastContainer.ShowToast("Список меню обновлен", Elements.ToastType.Success);
        }

        private async void DeleteMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int menuId)
            {
                var result = MessageBox.Show("Удалить меню?", "Подтверждение", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    var success = await AppData.ApiService.DeleteMenuAsync(menuId);
                    if (success)
                    {
                        await LoadAvailableMenus();
                        _weeklyMenu.Clear();
                        ToastContainer.ShowToast("Меню удалено", Elements.ToastType.Success);
                    }
                }
            }
        }

        private void OpenFavorites_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.mainWindow.OpenPages(new FavoritesPage());
        }

        private async void GenerateMenu_Click(object sender, RoutedEventArgs e)
        {
            await GenerateMenu();
        }
    }
}