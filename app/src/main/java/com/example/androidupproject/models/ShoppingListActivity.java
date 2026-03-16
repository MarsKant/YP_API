package com.example.androidupproject.models;

import android.annotation.SuppressLint;
import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.CheckBox;
import android.widget.ImageButton;
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

import java.io.FileOutputStream;
import java.io.IOException;
import java.util.ArrayList;
import java.util.List;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class ShoppingListActivity extends AppCompatActivity {

    private RecyclerView recyclerView;
    private ShopAdapter adapter;
    private SessionManager sessionManager;
    private TextView tvTitle;
    private Button btnSaveToFile, btnGenerateList;
    private int currentMenuId = 0; // Нужно получать из интента или меню

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_shopping_list);

        sessionManager = new SessionManager(this);
        tvTitle = findViewById(R.id.tvShopListName);
        recyclerView = findViewById(R.id.rvShoppingList);
        btnSaveToFile = findViewById(R.id.btnSaveToFile);
        btnGenerateList = findViewById(R.id.btnGenerateList); // Добавьте эту кнопку в layout

        recyclerView.setLayoutManager(new LinearLayoutManager(this));
        adapter = new ShopAdapter();
        recyclerView.setAdapter(adapter);

        BottomNavigationView bottomNav = findViewById(R.id.bottom_navigation);
        NavigationHelper.setupNavigation(this, bottomNav, R.id.nav_shop);

        btnSaveToFile.setOnClickListener(v -> saveListToFile());
        btnGenerateList.setOnClickListener(v -> generateShoppingList());

        // Получаем menuId из интента (если передается)
        currentMenuId = getIntent().getIntExtra("MENU_ID", 0);

        loadShoppingList();
    }

    private void loadShoppingList() {
        ApiClient.getService().getShoppingList(sessionManager.getUserId()).enqueue(new Callback<ApiResponse<ShoppingListDto>>() {
            @Override
            public void onResponse(Call<ApiResponse<ShoppingListDto>> call, Response<ApiResponse<ShoppingListDto>> response) {
                if (response.isSuccessful() && response.body() != null && response.body().data != null) {
                    ShoppingListDto list = response.body().data;
                    tvTitle.setText("🛒 " + list.name);
                    adapter.setItems(list.items);
                } else {
                    tvTitle.setText("Список покупок пуст");
                    adapter.setItems(new ArrayList<>());
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<ShoppingListDto>> call, Throwable t) {
                Toast.makeText(ShoppingListActivity.this, "Ошибка сети", Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void generateShoppingList() {
        if (currentMenuId == 0) {
            Toast.makeText(this, "Сначала создайте меню", Toast.LENGTH_SHORT).show();
            return;
        }

        btnGenerateList.setEnabled(false);
        btnGenerateList.setText("⏳ Генерация...");

        ApiClient.getService().generateShoppingList(currentMenuId, sessionManager.getUserId()).enqueue(new Callback<ApiResponse<Void>>() {
            @Override
            public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                btnGenerateList.setEnabled(true);
                btnGenerateList.setText("🔄 Сгенерировать список");

                if (response.isSuccessful() && response.body() != null && response.body().success) {
                    Toast.makeText(ShoppingListActivity.this, "Список покупок создан!", Toast.LENGTH_SHORT).show();
                    loadShoppingList();

                    // НАЧИСЛЯЕМ ОЧКИ за генерацию списка покупок
                    addPointsForAction("shopping_list_generated", 20);
                } else {
                    Toast.makeText(ShoppingListActivity.this, "Ошибка при создании списка", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {
                btnGenerateList.setEnabled(true);
                btnGenerateList.setText("🔄 Сгенерировать список");
                Toast.makeText(ShoppingListActivity.this, "Ошибка сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void saveListToFile() {
        List<ShoppingListItemDto> items = adapter.getItems();
        if (items == null || items.isEmpty()) {
            Toast.makeText(this, "Нечего сохранять", Toast.LENGTH_SHORT).show();
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.append("СПИСОК ПОКУПОК\n");
        sb.append("====================\n");
        sb.append("Дата: ").append(new java.text.SimpleDateFormat("dd.MM.yyyy HH:mm").format(new java.util.Date())).append("\n\n");

        int purchasedCount = 0;
        for (ShoppingListItemDto item : items) {
            String mark = item.isPurchased ? "[X]" : "[ ]";
            sb.append(mark).append(" ").append(item.name)
                    .append(" - ").append(item.quantity).append(" ").append(item.unit).append("\n");
            if (item.isPurchased) purchasedCount++;
        }

        sb.append("\n").append("Куплено: ").append(purchasedCount).append("/").append(items.size());

        String fileName = "shopping_list_" + System.currentTimeMillis() + ".txt";
        try (FileOutputStream fos = openFileOutput(fileName, MODE_PRIVATE)) {
            fos.write(sb.toString().getBytes());
            Toast.makeText(this, "✅ Сохранено: " + fileName, Toast.LENGTH_LONG).show();

            // НАЧИСЛЯЕМ ОЧКИ за сохранение списка (бонус)
            addPointsForAction("list_saved", 5);

        } catch (IOException e) {
            Log.e("SHOPPING_LIST", "Error saving file", e);
            Toast.makeText(this, "❌ Ошибка записи: " + e.getMessage(), Toast.LENGTH_SHORT).show();
        }
    }

    private void addPointsForAction(String action, int points) {
        // В зависимости от действия вызываем нужный метод
        Call<PointsResponse> call = null;

        switch(action) {
            case "shopping_list_generated":
                call = ApiClient.getService().shoppingListGenerated(sessionManager.getUserId());
                break;
            case "list_saved":
                // Можно добавить отдельный метод или использовать существующий
                call = ApiClient.getService().productAdded(sessionManager.getUserId()); // временно
                break;
        }

        if (call != null) {
            call.enqueue(new Callback<PointsResponse>() {
                @Override
                public void onResponse(Call<PointsResponse> call, Response<PointsResponse> response) {
                    if (response.isSuccessful() && response.body() != null) {
                        String message = response.body().message;
                        // Показываем уведомление только если это повышение уровня
                        if (message.contains("УРОВЕНЬ")) {
                            Toast.makeText(ShoppingListActivity.this, "🎉 " + message, Toast.LENGTH_LONG).show();
                        } else {
                            // Можно показывать короткое уведомление или не показывать
                            Log.d("POINTS", message);
                        }
                    }
                }

                @Override
                public void onFailure(Call<PointsResponse> call, Throwable t) {
                    Log.e("POINTS", "Error adding points", t);
                }
            });
        }
    }

    class ShopAdapter extends RecyclerView.Adapter<ShopAdapter.ViewHolder> {
        private List<ShoppingListItemDto> items = new ArrayList<>();

        public void setItems(List<ShoppingListItemDto> items) {
            this.items = items;
            notifyDataSetChanged();
        }

        public List<ShoppingListItemDto> getItems() { return items; }

        @NonNull
        @Override
        public ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
            View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_shopping, parent, false);
            return new ViewHolder(view);
        }

        @Override
        public void onBindViewHolder(@NonNull ViewHolder holder, @SuppressLint("RecyclerView") int position) {
            ShoppingListItemDto item = items.get(position);
            holder.tvName.setText(item.name);
            holder.tvQuantity.setText(item.quantity + " " + item.unit);

            holder.cbPurchased.setOnCheckedChangeListener(null);
            holder.cbPurchased.setChecked(item.isPurchased);
            holder.cbPurchased.setOnCheckedChangeListener((buttonView, isChecked) -> {
                item.isPurchased = isChecked;
                ApiClient.getService().toggleShoppingItem(item.id).enqueue(new Callback<ApiResponse<Void>>() {
                    @Override
                    public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                        if (response.isSuccessful()) {
                            // НАЧИСЛЯЕМ ОЧКИ за отметку товара
                            if (isChecked) {
                                addPointsForAction("item_purchased", 2);
                            }
                        }
                    }
                    @Override
                    public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {}
                });
            });

            holder.btnDelete.setOnClickListener(v -> {
                ApiClient.getService().deleteShoppingItem(item.id).enqueue(new Callback<ApiResponse<Void>>() {
                    @Override
                    public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                        if (response.isSuccessful()) {
                            items.remove(position);
                            notifyItemRemoved(position);
                            notifyItemRangeChanged(position, items.size());
                            Toast.makeText(ShoppingListActivity.this, "🗑️ Удалено", Toast.LENGTH_SHORT).show();

                            // НАЧИСЛЯЕМ ОЧКИ за удаление (можно и не начислять, но добавим для примера)
                            addPointsForAction("item_deleted", 1);

                        } else {
                            Toast.makeText(ShoppingListActivity.this, "Ошибка: " + response.code(), Toast.LENGTH_SHORT).show();
                        }
                    }
                    @Override
                    public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {
                        Toast.makeText(ShoppingListActivity.this, "Ошибка сети", Toast.LENGTH_SHORT).show();
                    }
                });
            });
        }

        @Override
        public int getItemCount() { return items.size(); }

        class ViewHolder extends RecyclerView.ViewHolder {
            TextView tvName, tvQuantity;
            CheckBox cbPurchased;
            ImageButton btnDelete;

            public ViewHolder(@NonNull View itemView) {
                super(itemView);
                tvName = itemView.findViewById(R.id.tvItemName);
                tvQuantity = itemView.findViewById(R.id.tvItemQuantity);
                cbPurchased = itemView.findViewById(R.id.cbPurchased);
                btnDelete = itemView.findViewById(R.id.btnDeleteShopItem);
            }
        }
    }
}