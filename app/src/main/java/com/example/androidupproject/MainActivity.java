package com.example.androidupproject;

import android.annotation.SuppressLint;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ImageButton;
import android.widget.TextView;
import android.widget.Toast;
import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.example.androidupproject.models.IngredientDto;
import com.example.androidupproject.models.InventoryAddRequest;
import com.example.androidupproject.models.InventoryItemDto;
import com.example.androidupproject.models.InventoryResponse;
import com.example.androidupproject.models.SessionManager;
import com.example.androidupproject.models.SimpleResponse;
import com.example.androidupproject.network.ApiClient;
import com.example.androidupproject.network.ApiResponse;
import com.google.android.material.bottomnavigation.BottomNavigationView; // Импорт для нижней панели

import java.util.ArrayList;
import java.util.List;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class MainActivity extends AppCompatActivity {

    private RecyclerView recyclerView;
    private ProductAdapter adapter;
    private SessionManager sessionManager;
    private EditText etNewProduct;

    @SuppressLint("MissingInflatedId")
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        sessionManager = new SessionManager(this);

        recyclerView = findViewById(R.id.rvProducts);
        etNewProduct = findViewById(R.id.etNewProduct);
        Button btnAdd = findViewById(R.id.btnAdd);

        recyclerView.setLayoutManager(new LinearLayoutManager(this));
        adapter = new ProductAdapter();
        recyclerView.setAdapter(adapter);

        BottomNavigationView bottomNav = findViewById(R.id.bottom_navigation);
        NavigationHelper.setupNavigation(this, bottomNav, R.id.nav_fridge);
        // ------------------------------------------------------------------------

        loadFridge();

        btnAdd.setOnClickListener(v -> addProduct());
    }

    private void loadFridge() {
        // ИСПРАВЛЕНИЕ: указываем InventoryResponse вместо ApiResponse<List<...>>
        ApiClient.getService().getUserInventory(sessionManager.getUserId()).enqueue(new Callback<InventoryResponse>() {
            @Override
            public void onResponse(Call<InventoryResponse> call, Response<InventoryResponse> response) {
                // В InventoryResponse список лежит в поле .data
                if (response.isSuccessful() && response.body() != null && response.body().success) {
                    adapter.setItems(response.body().data);
                }
            }
            @Override
            public void onFailure(Call<InventoryResponse> call, Throwable t) {
                Toast.makeText(MainActivity.this, "Ошибка загрузки", Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void addProduct() {
        String name = etNewProduct.getText().toString().trim();
        if (name.isEmpty()) return;

        InventoryAddRequest request = new InventoryAddRequest(name, 1.0, "шт");

        // ИСПРАВЛЕНИЕ: указываем SimpleResponse вместо ApiResponse<Void>
        ApiClient.getService().addToInventory(sessionManager.getUserId(), request)
                .enqueue(new Callback<SimpleResponse>() {
                    @Override
                    public void onResponse(Call<SimpleResponse> call, Response<SimpleResponse> response) {
                        if (response.isSuccessful() && response.body() != null && response.body().success) {
                            etNewProduct.setText("");
                            loadFridge();
                        }
                    }
                    @Override
                    public void onFailure(Call<SimpleResponse> call, Throwable t) {
                        Toast.makeText(MainActivity.this, "Ошибка сети", Toast.LENGTH_SHORT).show();
                    }
                });
    }

    class ProductAdapter extends RecyclerView.Adapter<ProductAdapter.ViewHolder> {
        // 1. Меняем тип списка на InventoryItemDto
        private List<InventoryItemDto> items = new ArrayList<>();

        public void setItems(List<InventoryItemDto> items) {
            this.items = items;
            notifyDataSetChanged();
        }

        @NonNull
        @Override
        public ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
            View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_product, parent, false);
            return new ViewHolder(view);
        }

        @Override
        public void onBindViewHolder(@NonNull ViewHolder holder, int position) {
            // 2. Работаем с объектом InventoryItemDto
            InventoryItemDto item = items.get(position);

            // Используем поле name из InventoryItemDto
            holder.tvName.setText(item.name);

            holder.btnDelete.setOnClickListener(v -> {
                // Передаем весь объект в метод удаления
                deleteProduct(item);
            });
        }

        // 3. Исправленный метод удаления (теперь принимает только один аргумент)
        private void deleteProduct(InventoryItemDto item) {
            // ИСПРАВЛЕНИЕ: передаем два аргумента (userId и ingredientId)
            // И используем SimpleResponse в Callback
            int userId = sessionManager.getUserId();
            android.util.Log.d("DEBUG_DELETE", "UserId: " + userId + ", IngredientId: " + item.ingredientId);
            ApiClient.getService().deleteInventoryItem(userId, item.ingredientId)
                    .enqueue(new Callback<SimpleResponse>() {
                        @Override
                        public void onResponse(Call<SimpleResponse> call, Response<SimpleResponse> response) {
                            if (response.isSuccessful() && response.body() != null && response.body().success) {
                                items.remove(item);
                                notifyDataSetChanged();
                                Toast.makeText(MainActivity.this, "Удалено", Toast.LENGTH_SHORT).show();
                            }
                        }
                        @Override
                        public void onFailure(Call<SimpleResponse> call, Throwable t) {
                            Toast.makeText(MainActivity.this, "Ошибка сети", Toast.LENGTH_SHORT).show();
                        }
                    });
        }

        @Override
        public int getItemCount() {
            return items.size();
        }

        class ViewHolder extends RecyclerView.ViewHolder {
            TextView tvName;
            ImageButton btnDelete;
            public ViewHolder(@NonNull View itemView) {
                super(itemView);
                tvName = itemView.findViewById(R.id.tvProductName);
                btnDelete = itemView.findViewById(R.id.btnDelete);
            }
        }
    }
}