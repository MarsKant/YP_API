package com.example.androidupproject.models;

import android.annotation.SuppressLint;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.CheckBox;
import android.widget.TextView;
import android.widget.Toast;
import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.example.androidupproject.NavigationHelper; // Импорт
import com.example.androidupproject.R;
import com.example.androidupproject.network.ApiClient;
import com.example.androidupproject.network.ApiResponse;
import com.google.android.material.bottomnavigation.BottomNavigationView; // Импорт

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

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_shopping_list);

        sessionManager = new SessionManager(this);
        tvTitle = findViewById(R.id.tvShopListName);
        recyclerView = findViewById(R.id.rvShoppingList);
        recyclerView.setLayoutManager(new LinearLayoutManager(this));
        adapter = new ShopAdapter();
        recyclerView.setAdapter(adapter);

        // --- ДОБАВЛЕНИЕ НАВИГАЦИИ ---
        BottomNavigationView bottomNav = findViewById(R.id.bottom_navigation);
        NavigationHelper.setupNavigation(this, bottomNav, R.id.nav_shop);
        // ---------------------------

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
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<ShoppingListDto>> call, Throwable t) {
                Toast.makeText(ShoppingListActivity.this, "Ошибка сети", Toast.LENGTH_SHORT).show();
            }
        });
    }

    class ShopAdapter extends RecyclerView.Adapter<ShopAdapter.ViewHolder> {
        private List<ShoppingListItemDto> items = new ArrayList<>();

        public void setItems(List<ShoppingListItemDto> items) {
            this.items = items;
            notifyDataSetChanged();
        }

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
                    public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {}
                    @Override
                    public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {
                        item.isPurchased = !isChecked;
                        notifyItemChanged(position);
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
            public ViewHolder(@NonNull View itemView) {
                super(itemView);
                tvName = itemView.findViewById(R.id.tvItemName);
                tvQuantity = itemView.findViewById(R.id.tvItemQuantity);
                cbPurchased = itemView.findViewById(R.id.cbPurchased);
            }
        }
    }
}