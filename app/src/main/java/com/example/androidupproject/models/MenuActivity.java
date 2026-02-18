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
import com.example.androidupproject.models.RecipeDetailActivity;

import java.util.ArrayList;
import java.util.List;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class MenuActivity extends AppCompatActivity {

    private List<MenuItemDto> menuItemsList = new ArrayList<>();
    private List<IngredientDto> ingredientDtoList = new ArrayList<>();
    private RecyclerView recyclerView;
    private MenuAdapter adapter;
    private SessionManager sessionManager;
    private int currentMenuId = 0;
    private TextView tvMenuTitle;

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

        // --- ИСПРАВЛЕНИЕ ЗДЕСЬ ---
        Button btnGenMenu = findViewById(R.id.btnGenerateMenu);
        btnGenMenu.setOnClickListener(v -> {
            Toast.makeText(MenuActivity.this, "Загрузка продуктов и генерация...", Toast.LENGTH_SHORT).show();

            // ВМЕСТО getFridge() и generateNewMenu() вызываем объединенный метод:
            getFridgeAndGenerate();
        });
        // -------------------------

        Button btnGenShop = findViewById(R.id.btnGenerateShop);
        btnGenShop.setOnClickListener(v -> generateShoppingList());

        Button btnClear = findViewById(R.id.btnClearMenu);
        btnClear.setOnClickListener(v -> {
            if (sessionManager.getUserId() == 0) {
                Toast.makeText(MenuActivity.this, "Ошибка: пользователь не найден", Toast.LENGTH_SHORT).show();
                return;
            }
            clearMenu();
        });

        loadMenu();
    }

    // Этот метод теперь используется и работает правильно (последовательно)
    private void getFridgeAndGenerate() {
        ApiClient.getService().getFridge(sessionManager.getUserId()).enqueue(new Callback<ApiResponse<List<IngredientDto>>>() {
            @Override
            public void onResponse(Call<ApiResponse<List<IngredientDto>>> call, Response<ApiResponse<List<IngredientDto>>> response) {
                if (response.isSuccessful() && response.body() != null && response.body().data != null) {
                    ingredientDtoList = response.body().data;

                    // Проверка: если холодильник пуст, предупреждаем, но пробуем генерировать
                    if (ingredientDtoList.isEmpty()) {
                        Toast.makeText(MenuActivity.this, "Холодильник пуст, генерируем случайное меню...", Toast.LENGTH_SHORT).show();
                    }

                    // Данные получены, ТЕПЕРЬ вызываем генерацию
                    generateNewMenu();
                } else {
                    Toast.makeText(MenuActivity.this, "Не удалось загрузить продукты", Toast.LENGTH_SHORT).show();
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<List<IngredientDto>>> call, Throwable t) {
                Toast.makeText(MenuActivity.this, "Ошибка сети при загрузке продуктов", Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void generateNewMenu() {
        ApiClient.getService().generateMenu(sessionManager.getUserId(), ingredientDtoList).enqueue(new Callback<ApiResponse<Void>>() {
            @Override
            public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                if (response.isSuccessful()) {
                    Toast.makeText(MenuActivity.this, "Меню готово!", Toast.LENGTH_SHORT).show();
                    loadMenu();
                } else {
                    // Код 502 придет сюда
                    Toast.makeText(MenuActivity.this, "Ошибка сервера: " + response.code(), Toast.LENGTH_LONG).show();
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {
                // Если сработал тайм-аут (долгое ожидание)
                Toast.makeText(MenuActivity.this, "ИИ думает слишком долго (Timeout)", Toast.LENGTH_LONG).show();
            }
        });
    }

    // Метод getFridge() удален, так как он больше не нужен (дублирует логику)

    private void clearMenu() {
        if (currentMenuId == 0) {
            Toast.makeText(this, "Меню еще не загружено или отсутствует", Toast.LENGTH_SHORT).show();
            return;
        }

        // Убедитесь, что в ApiService метод называется clearMenu (или deleteMenu, как мы обсуждали ранее)
        ApiClient.getService().clearMenu(currentMenuId).enqueue(new Callback<ApiResponse<Void>>() {
            @Override
            public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                if (response.isSuccessful() || response.code() == 404) {
                    adapter.setItems(new ArrayList<>());
                    tvMenuTitle.setText("Меню удалено");
                    currentMenuId = 0;
                    Toast.makeText(MenuActivity.this, "Меню успешно удалено!", Toast.LENGTH_SHORT).show();
                } else {
                    Toast.makeText(MenuActivity.this, "Ошибка удаления: " + response.code(), Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {
                Toast.makeText(MenuActivity.this, "Ошибка сети", Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void loadMenu() {
        ApiClient.getService().getCurrentMenu(sessionManager.getUserId()).enqueue(new Callback<ApiResponse<MenuDto>>() {
            @Override
            public void onResponse(Call<ApiResponse<MenuDto>> call, Response<ApiResponse<MenuDto>> response) {
                if (response.isSuccessful() && response.body() != null && response.body().data != null) {
                    MenuDto menu = response.body().data;
                    currentMenuId = menu.id;
                    adapter.setItems(menu.items);
                    tvMenuTitle.setText(menu.name != null ? menu.name : "Меню");
                } else {
                    tvMenuTitle.setText("Меню не найдено");
                    adapter.setItems(new ArrayList<>());
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<MenuDto>> call, Throwable t) {
                Toast.makeText(MenuActivity.this, "Ошибка сети при загрузке", Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void generateShoppingList() {
        if (currentMenuId == 0) {
            Toast.makeText(this, "Сначала сгенерируйте меню!", Toast.LENGTH_SHORT).show();
            return;
        }
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
            if (items == null || position >= items.size()) return;
            MenuItemDto item = items.get(position);
            holder.tvDateMeal.setText(item.date + " (" + item.mealType + ")");
            holder.tvRecipe.setText(item.recipeTitle);

            holder.btnOpen.setOnClickListener(v -> {
                Intent intent = new Intent(holder.itemView.getContext(), com.example.androidupproject.models.RecipeDetailActivity.class);
                intent.putExtra("RECIPE_ID", item.recipeId);
                holder.itemView.getContext().startActivity(intent);
            });
        }

        @Override
        public int getItemCount() { return (items != null) ? items.size() : 0; }

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