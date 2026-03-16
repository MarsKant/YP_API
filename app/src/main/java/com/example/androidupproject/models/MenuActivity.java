package com.example.androidupproject.models;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;
import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.example.androidupproject.NavigationHelper;
import com.example.androidupproject.R;
import com.example.androidupproject.network.ApiClient;
import com.example.androidupproject.network.ApiResponse;
import com.example.androidupproject.network.GenerateMenuResponse;
import com.example.androidupproject.network.MenuDetailsResponse;
import com.google.android.material.bottomnavigation.BottomNavigationView;

import java.util.ArrayList;
import java.util.List;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class MenuActivity extends AppCompatActivity {

    private RecyclerView recyclerView;
    private MenuAdapter adapter;
    private SessionManager sessionManager;
    private int currentMenuId = 0;
    private TextView tvMenuTitle;
    private List<InventoryItemDto> inventoryItems = new ArrayList<>();

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_menu);

        sessionManager = new SessionManager(this);
        tvMenuTitle = findViewById(R.id.tvMenuTitle);
        recyclerView = findViewById(R.id.rvMenu);
        recyclerView.setLayoutManager(new LinearLayoutManager(this));

        adapter = new MenuAdapter();
        recyclerView.setAdapter(adapter);

        BottomNavigationView bottomNav = findViewById(R.id.bottom_navigation);
        NavigationHelper.setupNavigation(this, bottomNav, R.id.nav_menu);

        loadInventory();

        Button btnGenMenu = findViewById(R.id.btnGenerateMenu);
        btnGenMenu.setOnClickListener(v -> generateNewMenu());

        Button btnGenShop = findViewById(R.id.btnGenerateShop);
        btnGenShop.setOnClickListener(v -> generateShoppingList());

        Button btnClear = findViewById(R.id.btnClearMenu);
        btnClear.setOnClickListener(v -> clearMenu());

        loadMenus();
    }

    private List<IngredientDto> convertToIngredientDto(List<InventoryItemDto> inventoryItems) {
        List<IngredientDto> result = new ArrayList<>();
        for (InventoryItemDto item : inventoryItems) {
            IngredientDto dto = new IngredientDto();
            dto.name = item.name;
            dto.quantity = item.quantity;
            dto.unit = item.unit;
            dto.category = item.category;
            dto.ingredientId = item.ingredientId;
            dto.id = item.id;
            result.add(dto);
        }
        return result;
    }

    private void generateNewMenu() {
        int userId = sessionManager.getUserId();
        Log.d("MENU_DEBUG", "Generating new menu for user: " + sessionManager.getUserId());

        if (inventoryItems.isEmpty()) {
            Toast.makeText(this, "Добавьте ингредиенты в инвентарь", Toast.LENGTH_SHORT).show();
            return;
        }

        List<IngredientDto> ingredientsToSend = convertToIngredientDto(inventoryItems);

        ApiClient.getService().generateMenu(sessionManager.getUserId(), ingredientsToSend)
                .enqueue(new Callback<GenerateMenuResponse>() {
                    @Override
                    public void onResponse(Call<GenerateMenuResponse> call, Response<GenerateMenuResponse> response) {
                        Log.d("MENU_DEBUG", "Generate menu response code: " + response.code());

                        if (response.isSuccessful() && response.body() != null && response.body().menuId > 0) {
                            GenerateMenuResponse genResponse = response.body();
                            Log.d("MENU_DEBUG", "Generate menu response: success=" + genResponse.success +
                                    ", menuId=" + genResponse.menuId);
                            Toast.makeText(MenuActivity.this, "Меню готово!", Toast.LENGTH_SHORT).show();
                            loadMenus();
                            ApiClient.getService().menuCreated(sessionManager.getUserId()).enqueue(new Callback<PointsResponse>() {
                                @Override
                                public void onResponse(Call<PointsResponse> call, Response<PointsResponse> response) {
                                    if (response.isSuccessful() && response.body() != null) {
                                        Toast.makeText(MenuActivity.this, response.body().message, Toast.LENGTH_SHORT).show();
                                    }
                                }

                                @Override
                                public void onFailure(Call<PointsResponse> call, Throwable t) {
                                }
                            });
                        }
                    }

                    @Override
                    public void onFailure(Call<GenerateMenuResponse> call, Throwable t) {
                        Log.e("MENU_DEBUG", "Network failure: " + t.getMessage(), t);
                        Toast.makeText(MenuActivity.this,
                                "Ошибка сети: " + t.getMessage(), Toast.LENGTH_LONG).show();
                    }
                });
    }

    private void loadMenus() {
        Log.d("MENU_DEBUG", "Loading menus for user: " + sessionManager.getUserId());

        ApiClient.getService().getUserMenus(sessionManager.getUserId()).enqueue(new Callback<List<MenuDto>>() {
            @Override
            public void onResponse(Call<List<MenuDto>> call, Response<List<MenuDto>> response) {
                Log.d("MENU_DEBUG", "Load menus response code: " + response.code());

                if (response.isSuccessful() && response.body() != null) {
                    List<MenuDto> menus = response.body();
                    Log.d("MENU_DEBUG", "Received " + menus.size() + " menus");

                    if (!menus.isEmpty()) {
                        MenuDto menu = menus.get(0);
                        Log.d("MENU_DEBUG", "Selected menu: id=" + menu.id + ", name=" + menu.name);

                        currentMenuId = menu.id;
                        tvMenuTitle.setText(menu.name != null ? menu.name : "Ваше меню");

                        loadMenuDetails(menu.id);
                    } else {
                        Log.d("MENU_DEBUG", "No menus found");
                        tvMenuTitle.setText("Меню не найдено");
                        adapter.setItems(new ArrayList<>());
                    }
                } else {
                    Log.e("MENU_DEBUG", "Failed to load menus. Code: " + response.code());
                    tvMenuTitle.setText("Ошибка загрузки меню");
                    adapter.setItems(new ArrayList<>());
                }
            }

            @Override
            public void onFailure(Call<List<MenuDto>> call, Throwable t) {
                Log.e("MENU_DEBUG", "Network failure loading menus: " + t.getMessage(), t);
                Toast.makeText(MenuActivity.this, "Ошибка сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
                tvMenuTitle.setText("Ошибка сети");
                adapter.setItems(new ArrayList<>());
            }
        });
    }
    private void loadMenuDetails(int menuId) {
        Log.d("MENU_DEBUG", "Loading details for menuId: " + menuId);

        ApiClient.getService().getMenuDetails(menuId).enqueue(new Callback<MenuDetailsResponse>() {
            @Override
            public void onResponse(Call<MenuDetailsResponse> call, Response<MenuDetailsResponse> response) {
                Log.d("MENU_DEBUG", "Menu details response code: " + response.code());

                if (response.isSuccessful() && response.body() != null) {
                    MenuDetailsResponse detailsResponse = response.body();
                    Log.d("MENU_DEBUG", "Menu details success: " + detailsResponse.success);

                    if (detailsResponse.success && detailsResponse.data != null) {
                        MenuDto menu = detailsResponse.data;
                        Log.d("MENU_DEBUG", "Menu items count: " +
                                (menu.items != null ? menu.items.size() : "null"));

                        if (menu.items != null && !menu.items.isEmpty()) {
                            adapter.setItems(menu.items);
                        } else {
                            adapter.setItems(new ArrayList<>());
                        }
                    } else {
                        adapter.setItems(new ArrayList<>());
                    }
                } else {
                    Log.e("MENU_DEBUG", "Failed to load menu details");
                    adapter.setItems(new ArrayList<>());
                }
            }

            @Override
            public void onFailure(Call<MenuDetailsResponse> call, Throwable t) {
                Log.e("MENU_DEBUG", "Network failure loading details: " + t.getMessage(), t);
                adapter.setItems(new ArrayList<>());
            }
        });
    }
    private void clearMenu() {
        if (currentMenuId == 0) {
            Toast.makeText(this, "Нет меню для удаления", Toast.LENGTH_SHORT).show();
            return;
        }

        Log.d("MENU_DEBUG", "Deleting menu: " + currentMenuId);

        ApiClient.getService().deleteMenu(currentMenuId).enqueue(new Callback<SimpleResponse>() {
            @Override
            public void onResponse(Call<SimpleResponse> call, Response<SimpleResponse> response) {
                Log.d("MENU_DEBUG", "Delete menu response code: " + response.code());

                if (response.isSuccessful() && response.body() != null) {
                    SimpleResponse simpleResponse = response.body();
                    if (simpleResponse.success) {
                        adapter.setItems(new ArrayList<>());
                        tvMenuTitle.setText("Меню удалено");
                        currentMenuId = 0;
                        Toast.makeText(MenuActivity.this, "Меню удалено", Toast.LENGTH_SHORT).show();
                    } else {
                        Toast.makeText(MenuActivity.this, "Ошибка при удалении", Toast.LENGTH_SHORT).show();
                    }
                } else {
                    Toast.makeText(MenuActivity.this, "Ошибка сервера", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<SimpleResponse> call, Throwable t) {
                Log.e("MENU_DEBUG", "Network failure deleting menu: " + t.getMessage(), t);
                Toast.makeText(MenuActivity.this, "Ошибка сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }
    private void generateShoppingList() {
        if (currentMenuId == 0) {
            Toast.makeText(this, "Сначала создайте меню", Toast.LENGTH_SHORT).show();
            return;
        }

        Log.d("MENU_DEBUG", "Generating shopping list for menu: " + currentMenuId);

        ApiClient.getService().generateShoppingList(currentMenuId, sessionManager.getUserId()).enqueue(new Callback<ApiResponse<Void>>() {
            @Override
            public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                Log.d("MENU_DEBUG", "Generate shopping list response code: " + response.code());

                if (response.isSuccessful() && response.body() != null && response.body().success) {
                    Toast.makeText(MenuActivity.this, "Список покупок создан!", Toast.LENGTH_SHORT).show();
                } else {
                    Toast.makeText(MenuActivity.this, "Ошибка при создании списка", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {
                Log.e("MENU_DEBUG", "Network failure: " + t.getMessage(), t);
                Toast.makeText(MenuActivity.this, "Ошибка сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }
    private void loadInventory() {
        Log.d("MENU_DEBUG", "Loading inventory for user: " + sessionManager.getUserId());

        ApiClient.getService().getUserInventory(sessionManager.getUserId())
                .enqueue(new Callback<InventoryResponse>() {
                    @Override
                    public void onResponse(Call<InventoryResponse> call, Response<InventoryResponse> response) {
                        if (response.isSuccessful() && response.body() != null && response.body().success) {
                            inventoryItems.clear();
                            if (response.body().data != null) {
                                inventoryItems.addAll(response.body().data);
                                Log.d("MENU_DEBUG", "Loaded " + inventoryItems.size() + " inventory items");
                            }
                        } else {
                            Log.e("MENU_DEBUG", "Failed to load inventory: " + response.code());
                        }
                    }

                    @Override
                    public void onFailure(Call<InventoryResponse> call, Throwable t) {
                        Log.e("MENU_DEBUG", "Network error loading inventory: " + t.getMessage());
                        Toast.makeText(MenuActivity.this, "Не удалось загрузить инвентарь", Toast.LENGTH_SHORT).show();
                    }
                });
    }

    class MenuAdapter extends RecyclerView.Adapter<MenuAdapter.ViewHolder> {
        private List<MenuItemDto> items = new ArrayList<>();

        public void setItems(List<MenuItemDto> items) {
            this.items = (items != null) ? items : new ArrayList<>();
            notifyDataSetChanged();
        }

        @NonNull
        @Override
        public ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
            View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_menu_meal, parent, false);
            return new ViewHolder(view);
        }

        @Override
        public void onBindViewHolder(@NonNull ViewHolder holder, int position) {
            try {
                MenuItemDto item = items.get(position);

                String dateStr = "";
                if (item.date != null) {
                    try {
                        String[] parts = item.date.split("T");
                        if (parts.length > 0) {
                            dateStr = parts[0] + " ";
                        }
                    } catch (Exception e) {
                        dateStr = item.date + " ";
                    }
                }

                holder.tvDateMeal.setText(dateStr + (item.mealType != null ? item.mealType : "Прием пищи"));
                holder.tvRecipe.setText(item.recipeTitle != null ? item.recipeTitle : "Рецепт");

                holder.btnOpen.setOnClickListener(v -> {
                    if (item.recipeId > 0) {
                        Intent intent = new Intent(MenuActivity.this, RecipeDetailActivity.class);
                        intent.putExtra("RECIPE_ID", item.recipeId);
                        startActivity(intent);
                    } else {
                        Toast.makeText(MenuActivity.this, "ID рецепта не указан", Toast.LENGTH_SHORT).show();
                    }
                });
            } catch (Exception e) {
                Log.e("MENU_DEBUG", "Error binding view at position " + position, e);
            }
        }

        @Override
        public int getItemCount() { return items.size(); }

        class ViewHolder extends RecyclerView.ViewHolder {
            TextView tvDateMeal, tvRecipe;
            Button btnOpen;

            public ViewHolder(View itemView) {
                super(itemView);
                tvDateMeal = itemView.findViewById(R.id.tvDateMeal);
                tvRecipe = itemView.findViewById(R.id.tvRecipeName);
                btnOpen = itemView.findViewById(R.id.btnOpenRecipe);
            }
        }
    }
}