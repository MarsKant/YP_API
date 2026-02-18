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
import com.example.androidupproject.models.SessionManager;
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
        ApiClient.getService().getFridge(sessionManager.getUserId()).enqueue(new Callback<ApiResponse<List<IngredientDto>>>() {
            @Override
            public void onResponse(Call<ApiResponse<List<IngredientDto>>> call, Response<ApiResponse<List<IngredientDto>>> response) {
                if (response.isSuccessful() && response.body() != null && response.body().data != null) {
                    adapter.setItems(response.body().data);
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<List<IngredientDto>>> call, Throwable t) {
                Toast.makeText(MainActivity.this, "Ошибка загрузки", Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void addProduct() {
        String name = etNewProduct.getText().toString().trim();
        if (name.isEmpty()) {
            Toast.makeText(this, "Введите название", Toast.LENGTH_SHORT).show();
            return;
        }

        IngredientDto newProduct = new IngredientDto(name, "шт");

        ApiClient.getService().addToFridge(sessionManager.getUserId(), newProduct)
                .enqueue(new Callback<ApiResponse<Void>>() {
                    @Override
                    public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                        if (response.isSuccessful()) {
                            etNewProduct.setText("");
                            Toast.makeText(MainActivity.this, "Добавлено!", Toast.LENGTH_SHORT).show();
                            loadFridge();
                        } else {
                            Toast.makeText(MainActivity.this, "Ошибка: " + response.code(), Toast.LENGTH_SHORT).show();
                        }
                    }

                    @Override
                    public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {
                        Toast.makeText(MainActivity.this, "Нет сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
                    }
                });
    }

    class ProductAdapter extends RecyclerView.Adapter<ProductAdapter.ViewHolder> {
        private List<IngredientDto> items = new ArrayList<>();

        public void setItems(List<IngredientDto> items) {
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
            IngredientDto item = items.get(position);
            holder.tvName.setText(item.name);
            holder.btnDelete.setOnClickListener(v -> {
                deleteProduct(item, item.id);
            });
        }

        private void deleteProduct(IngredientDto item, int ingredientId) {
            ApiClient.getService().removeFromFridge(sessionManager.getUserId(), ingredientId)
                    .enqueue(new Callback<ApiResponse<Void>>() {
                        @Override
                        public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                            if (response.isSuccessful()) {
                                items.remove(item);
                                notifyDataSetChanged();
                                Toast.makeText(MainActivity.this, "Удалено", Toast.LENGTH_SHORT).show();
                            } else {
                                Toast.makeText(MainActivity.this, "Ошибка удаления: " + response.code(), Toast.LENGTH_SHORT).show();
                            }
                        }
                        @Override
                        public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {
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