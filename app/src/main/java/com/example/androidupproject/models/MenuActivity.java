package com.example.androidupproject.models;

import android.content.Intent;
import android.os.Bundle;
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
import com.google.android.material.bottomnavigation.BottomNavigationView;
import com.example.androidupproject.models.*; // Импортируем все модели

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

    // ИСПРАВЛЕНО: Теперь используем актуальный список InventoryItemDto для холодильника
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

        Button btnGenMenu = findViewById(R.id.btnGenerateMenu);
        btnGenMenu.setOnClickListener(v -> getFridgeAndGenerate());

        Button btnGenShop = findViewById(R.id.btnGenerateShop);
        btnGenShop.setOnClickListener(v -> generateShoppingList());

        Button btnClear = findViewById(R.id.btnClearMenu);
        btnClear.setOnClickListener(v -> clearMenu());

        loadMenu();
    }

    private void getFridgeAndGenerate() {
        // ИСПРАВЛЕНО: getFridge теперь возвращает List<InventoryItemDto>
        ApiClient.getService().getUserInventory(sessionManager.getUserId()).enqueue(new Callback<InventoryResponse>() {
            @Override
            public void onResponse(Call<InventoryResponse> call, Response<InventoryResponse> response) {
                if (response.isSuccessful() && response.body() != null && response.body().success) {
                    inventoryItems = response.body().data; // Тип InventoryItemDto должен совпадать
                    generateNewMenu();
                }
            }

            @Override
            public void onFailure(Call<InventoryResponse> call, Throwable t) {

            }

        });
    }

    private void generateNewMenu() {
        // Используем SimpleResponse, так как именно его возвращает ApiService
        ApiClient.getService().generateMenu(sessionManager.getUserId())
                .enqueue(new Callback<SimpleResponse>() { // <-- ИСПРАВЛЕНО
                    @Override
                    public void onResponse(Call<SimpleResponse> call, Response<SimpleResponse> response) {
                        if (response.isSuccessful() && response.body() != null && response.body().success) {
                            Toast.makeText(MenuActivity.this, "Меню готово!", Toast.LENGTH_SHORT).show();
                            loadMenu();
                        } else {
                            // Если пришел BadRequest (например, не хватило ингредиентов), покажем это
                            Toast.makeText(MenuActivity.this, "Ошибка: рецепты не найдены", Toast.LENGTH_LONG).show();
                        }
                    }

                    @Override
                    public void onFailure(Call<SimpleResponse> call, Throwable t) {
                        Toast.makeText(MenuActivity.this, "Ошибка сети или тайм-аут", Toast.LENGTH_LONG).show();
                    }
                });
    }

    private void loadMenu() {
        // ИСПРАВЛЕНО: getCurrentMenu возвращает List<MenuDto>, а не один объект
        ApiClient.getService().getUserMenu(sessionManager.getUserId()).enqueue(new Callback<ApiResponse<List<MenuDto>>>() {
            @Override
            public void onResponse(Call<ApiResponse<List<MenuDto>>> call, Response<ApiResponse<List<MenuDto>>> response) {
                if (response.isSuccessful() && response.body() != null && !response.body().data.isEmpty()) {
                    // Берем первое меню из списка
                    MenuDto menu = response.body().data.get(0);
                    currentMenuId = menu.id;
                    adapter.setItems(menu.items);
                    tvMenuTitle.setText(menu.name != null ? menu.name : "Ваше меню");
                } else {
                    tvMenuTitle.setText("Меню не найдено");
                    adapter.setItems(new ArrayList<>());
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<List<MenuDto>>> call, Throwable t) {
                Toast.makeText(MenuActivity.this, "Ошибка загрузки меню", Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void clearMenu() {
        if (currentMenuId == 0) return;

        ApiClient.getService().deleteMenu(currentMenuId).enqueue(new Callback<ApiResponse<Void>>() {
            @Override
            public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                if (response.isSuccessful()) {
                    adapter.setItems(new ArrayList<>());
                    tvMenuTitle.setText("Меню удалено");
                    currentMenuId = 0;
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {}
        });
    }

    private void generateShoppingList() {
        if (currentMenuId == 0) return;
        ApiClient.getService().generateShoppingList(currentMenuId, sessionManager.getUserId()).enqueue(new Callback<ApiResponse<Void>>() {
            @Override
            public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                if (response.isSuccessful()) {
                    Toast.makeText(MenuActivity.this, "Список покупок создан!", Toast.LENGTH_SHORT).show();
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {}
        });
    }

    // Адаптер остается почти без изменений, только проверка на null для безопасности
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
            MenuItemDto item = items.get(position);
            // Проверьте, что в MenuItemDto есть эти поля (date, mealType, recipeTitle)
            holder.tvDateMeal.setText(item.date);
            holder.tvRecipe.setText(item.recipeTitle);

            holder.btnOpen.setOnClickListener(v -> {
                Intent intent = new Intent(MenuActivity.this, RecipeDetailActivity.class);
                intent.putExtra("RECIPE_ID", item.recipeId);
                startActivity(intent);
            });
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