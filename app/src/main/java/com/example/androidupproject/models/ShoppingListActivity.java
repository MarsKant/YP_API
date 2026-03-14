    package com.example.androidupproject.models;

    import android.annotation.SuppressLint;
    import android.os.Bundle;
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
        private Button btnSaveToFile;

        @Override
        protected void onCreate(Bundle savedInstanceState) {
            super.onCreate(savedInstanceState);
            setContentView(R.layout.activity_shopping_list);

            sessionManager = new SessionManager(this);
            tvTitle = findViewById(R.id.tvShopListName);
            recyclerView = findViewById(R.id.rvShoppingList);
            btnSaveToFile = findViewById(R.id.btnSaveToFile);

            recyclerView.setLayoutManager(new LinearLayoutManager(this));
            adapter = new ShopAdapter();
            recyclerView.setAdapter(adapter);

            BottomNavigationView bottomNav = findViewById(R.id.bottom_navigation);
            NavigationHelper.setupNavigation(this, bottomNav, R.id.nav_shop);

            btnSaveToFile.setOnClickListener(v -> saveListToFile());

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

        private void saveListToFile() {
            List<ShoppingListItemDto> items = adapter.getItems();
            if (items == null || items.isEmpty()) {
                Toast.makeText(this, "Нечего сохранять", Toast.LENGTH_SHORT).show();
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.append("СПИСОК ПОКУПОК\n====================\n");
            for (ShoppingListItemDto item : items) {
                String mark = item.isPurchased ? "[X] " : "[ ] ";
                sb.append(mark).append(item.name).append(" (").append(item.quantity).append(" ").append(item.unit).append(")\n");
            }

            String fileName = "shopping_list.txt";
            try (FileOutputStream fos = openFileOutput(fileName, MODE_PRIVATE)) {
                fos.write(sb.toString().getBytes());
                Toast.makeText(this, "Сохранено: " + fileName, Toast.LENGTH_LONG).show();
            } catch (IOException e) {
                Toast.makeText(this, "Ошибка записи", Toast.LENGTH_SHORT).show();
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
                        @Override public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {}
                        @Override public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {}
                    });
                });

                // ОБРАБОТКА УДАЛЕНИЯ
                holder.btnDelete.setOnClickListener(v -> {
                    ApiClient.getService().deleteShoppingItem(item.id).enqueue(new Callback<ApiResponse<Void>>() {
                        @Override
                        public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                            if (response.isSuccessful()) {
                                items.remove(position);
                                notifyItemRemoved(position);
                                notifyItemRangeChanged(position, items.size());
                                Toast.makeText(ShoppingListActivity.this, "Удалено", Toast.LENGTH_SHORT).show();
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
                ImageButton btnDelete; // Кнопка удаления

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