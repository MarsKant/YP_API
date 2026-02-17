package com.example.androidupproject.models;

import android.annotation.SuppressLint;
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

public class FavoritesActivity extends AppCompatActivity {

    private RecyclerView recyclerView;
    private FavoritesAdapter adapter;
    private SessionManager sessionManager;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_favorites);

        sessionManager = new SessionManager(this);
        recyclerView = findViewById(R.id.rvFavorites);
        recyclerView.setLayoutManager(new LinearLayoutManager(this));
        adapter = new FavoritesAdapter();
        recyclerView.setAdapter(adapter);

        // --- ДОБАВЛЕНИЕ НАВИГАЦИИ ---
        BottomNavigationView bottomNav = findViewById(R.id.bottom_navigation);
        NavigationHelper.setupNavigation(this, bottomNav, R.id.nav_fav);
        // ---------------------------

        loadFavorites();
    }

    private void loadFavorites() {
        ApiClient.getService().getFavorites(sessionManager.getUserId()).enqueue(new Callback<ApiResponse<List<RecipeDto>>>() {
            @Override
            public void onResponse(Call<ApiResponse<List<RecipeDto>>> call, Response<ApiResponse<List<RecipeDto>>> response) {
                if (response.isSuccessful() && response.body() != null && response.body().data != null) {
                    adapter.setItems(response.body().data);
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<List<RecipeDto>>> call, Throwable t) {
                Toast.makeText(FavoritesActivity.this, "Ошибка загрузки", Toast.LENGTH_SHORT).show();
            }
        });
    }

    class FavoritesAdapter extends RecyclerView.Adapter<FavoritesAdapter.ViewHolder> {
        private List<RecipeDto> items = new ArrayList<>();

        public void setItems(List<RecipeDto> items) {
            this.items = items;
            notifyDataSetChanged();
        }

        @NonNull
        @Override
        public ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
            View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_favorite, parent, false);
            return new ViewHolder(view);
        }

        @Override
        public void onBindViewHolder(@NonNull ViewHolder holder, @SuppressLint("RecyclerView") int position) {
            RecipeDto item = items.get(position);
            holder.tvTitle.setText(item.title);
            holder.tvDesc.setText(item.description);

            holder.itemView.setOnClickListener(v -> {
                Intent intent = new Intent(FavoritesActivity.this, com.example.androidupproject.RecipeDetailActivity.class);
                intent.putExtra("RECIPE_ID", item.id);
                startActivity(intent);
            });

            holder.btnRemove.setOnClickListener(v -> {
                ApiClient.getService().toggleFavorite(item.id, sessionManager.getUserId()).enqueue(new Callback<ApiResponse<Void>>() {
                    @Override
                    public void onResponse(Call<ApiResponse<Void>> call, Response<ApiResponse<Void>> response) {
                        if (response.isSuccessful()) {
                            items.remove(position);
                            notifyItemRemoved(position);
                            notifyItemRangeChanged(position, items.size());
                            Toast.makeText(FavoritesActivity.this, "Удалено", Toast.LENGTH_SHORT).show();
                        }
                    }
                    @Override
                    public void onFailure(Call<ApiResponse<Void>> call, Throwable t) {}
                });
            });
        }

        @Override
        public int getItemCount() { return items.size(); }

        class ViewHolder extends RecyclerView.ViewHolder {
            TextView tvTitle, tvDesc;
            Button btnRemove;
            public ViewHolder(@NonNull View itemView) {
                super(itemView);
                tvTitle = itemView.findViewById(R.id.tvFavTitle);
                tvDesc = itemView.findViewById(R.id.tvFavDesc);
                btnRemove = itemView.findViewById(R.id.btnRemoveFav);
            }
        }
    }
}