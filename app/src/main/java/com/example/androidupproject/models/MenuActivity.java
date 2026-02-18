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

import com.example.androidupproject.MainActivity;
import com.example.androidupproject.NavigationHelper; // Импорт помощника
import com.example.androidupproject.R;
import com.example.androidupproject.models.MenuDto;
import com.example.androidupproject.models.MenuItemDto;
import com.example.androidupproject.models.SessionManager;
import com.example.androidupproject.network.ApiClient;
import com.example.androidupproject.network.ApiResponse;
import com.google.android.material.bottomnavigation.BottomNavigationView; // Импорт меню

import java.util.ArrayList;
import java.util.List;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class MenuActivity extends AppCompatActivity {

    private List<MenuItemDto> menuItemsList = new ArrayList<>();
    private  List<IngredientDto> ingredientDtoList = new ArrayList<>();
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
        Button btnGenMenu = findViewById(R.id.btnGenerateMenu);
        btnGenMenu.setOnClickListener(v -> {
            Toast.makeText(MenuActivity.this, "Генерируем меню...", Toast.LENGTH_SHORT).show();
            getFridge();
            generateNewMenu();
        });

        Button btnGenShop = findViewById(R.id.btnGenerateShop);
        btnGenShop.setOnClickListener(v -> generateShoppingList());

        loadMenu();
    }

    private void getFridge() {
        ApiClient.getService().getFridge(sessionManager.getUserId()).enqueue(new Callback<ApiResponse<List<IngredientDto>>>() {
            @Override
            public void onResponse(Call<ApiResponse<List<IngredientDto>>> call, Response<ApiResponse<List<IngredientDto>>> response) {
                if (response.isSuccessful() && response.body() != null && response.body().data != null) {
                    ingredientDtoList = response.body().data;
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<List<IngredientDto>>> call, Throwable t) {
                Toast.makeText(MenuActivity.this, "Ошибка загрузки", Toast.LENGTH_SHORT).show();
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
                    Toast.makeText(MenuActivity.this, "Ошибка генерации: " + response.code(), Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {
                Toast.makeText(MenuActivity.this, "Ошибка сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
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
                    Toast.makeText(MenuActivity.this, "Меню нет. Нажмите кнопку генерации.", Toast.LENGTH_LONG).show();
                }
            }

            @Override
            public void onFailure(Call<ApiResponse<MenuDto>> call, Throwable t) {
                Toast.makeText(MenuActivity.this, "Ошибка сети", Toast.LENGTH_SHORT).show();
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

    // Адаптер (без изменений, но включен для целостности)
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
                Intent intent = new Intent(MenuActivity.this, com.example.androidupproject.RecipeDetailActivity.class);
                intent.putExtra("RECIPE_ID", item.recipeId);
                startActivity(intent);
            });
        }

        @Override
        public int getItemCount() {
            return (items != null) ? items.size() : 0;
        }

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