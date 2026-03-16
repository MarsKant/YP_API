package com.example.androidupproject.models;

import android.os.Bundle;
import android.util.Log;
import android.widget.Button;
import android.widget.ImageView;
import android.widget.TextView;
import android.widget.Toast;
import androidx.appcompat.app.AppCompatActivity;

import com.bumptech.glide.Glide;
import com.bumptech.glide.load.engine.DiskCacheStrategy;
import com.example.androidupproject.R;
import com.example.androidupproject.network.ApiClient;
import com.example.androidupproject.network.ApiResponse;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class RecipeDetailActivity extends AppCompatActivity {

    private int recipeId;
    private String recipeTitle;
    private TextView tvTitle, tvDesc, tvIngredients, tvInstructions;
    private ImageView ivImage;
    private Button btnGenerateImage, btnParseImage;
    private SessionManager sessionManager;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_recipe_detail);

        sessionManager = new SessionManager(this);
        recipeId = getIntent().getIntExtra("RECIPE_ID", 0);
        recipeTitle = getIntent().getStringExtra("RECIPE_TITLE");

        tvTitle = findViewById(R.id.tvRecipeTitle);
        tvDesc = findViewById(R.id.tvDescription);
        tvIngredients = findViewById(R.id.tvIngredients);
        tvInstructions = findViewById(R.id.tvInstructions);
        ivImage = findViewById(R.id.ivRecipeImage);
        btnGenerateImage = findViewById(R.id.btnGenerateImage);
        btnParseImage = findViewById(R.id.btnParseImage);

        btnGenerateImage.setOnClickListener(v -> generateImage());
        btnParseImage.setOnClickListener(v -> parseImageFromPovar());

        loadRecipe();
    }

    private void generateImage() {
        if (recipeTitle == null || recipeTitle.isEmpty()) {
            Toast.makeText(this, "Нет названия рецепта", Toast.LENGTH_SHORT).show();
            return;
        }

        btnGenerateImage.setEnabled(false);
        btnGenerateImage.setText("Генерация...");

        ImageGenerationRequest request = new ImageGenerationRequest(recipeTitle, recipeId);

        ApiClient.getService().generateRecipeImage(request).enqueue(new Callback<ImageGenerationResponse>() {
            @Override
            public void onResponse(Call<ImageGenerationResponse> call, Response<ImageGenerationResponse> response) {
                btnGenerateImage.setEnabled(true);
                btnGenerateImage.setText("Сгенерировать");

                if (response.isSuccessful() && response.body() != null) {
                    String imageUrl = response.body().url;
                    loadImage(imageUrl);
                    Toast.makeText(RecipeDetailActivity.this, "Изображение сгенерировано!", Toast.LENGTH_SHORT).show();
                } else {
                    Toast.makeText(RecipeDetailActivity.this, "Ошибка генерации", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ImageGenerationResponse> call, Throwable t) {
                btnGenerateImage.setEnabled(true);
                btnGenerateImage.setText("Сгенерировать");
                Toast.makeText(RecipeDetailActivity.this, "Ошибка сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void parseImageFromPovar() {
        if (recipeTitle == null || recipeTitle.isEmpty()) {
            Toast.makeText(this, "Нет названия рецепта", Toast.LENGTH_SHORT).show();
            return;
        }

        btnParseImage.setEnabled(false);
        btnParseImage.setText("Поиск...");

        ApiClient.getService().parsePovarImage(recipeTitle, recipeId).enqueue(new Callback<ImageParseResponse>() {
            @Override
            public void onResponse(Call<ImageParseResponse> call, Response<ImageParseResponse> response) {
                btnParseImage.setEnabled(true);
                btnParseImage.setText("Найти на Povar.ru");

                if (response.isSuccessful() && response.body() != null) {
                    String imageUrl = response.body().url;
                    loadImage(imageUrl);
                    Toast.makeText(RecipeDetailActivity.this, "Изображение найдено!", Toast.LENGTH_SHORT).show();
                } else {
                    Toast.makeText(RecipeDetailActivity.this, "Изображение не найдено", Toast.LENGTH_SHORT).show();
                }
            }

            @Override
            public void onFailure(Call<ImageParseResponse> call, Throwable t) {
                btnParseImage.setEnabled(true);
                btnParseImage.setText("Найти на Povar.ru");
                Toast.makeText(RecipeDetailActivity.this, "Ошибка сети: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void loadRecipe() {
        ApiClient.getService().getRecipeById(recipeId).enqueue(new Callback<ApiResponse<RecipeDto>>() {
            @Override
            public void onResponse(Call<ApiResponse<RecipeDto>> call, Response<ApiResponse<RecipeDto>> response) {
                if (response.isSuccessful() && response.body() != null && response.body().data != null) {
                    displayRecipe(response.body().data);

                    // Начисляем очки за просмотр рецепта
                    ApiClient.getService().recipeViewed(sessionManager.getUserId()).enqueue(new Callback<PointsResponse>() {
                        @Override
                        public void onResponse(Call<PointsResponse> call, Response<PointsResponse> response) {
                            if (response.isSuccessful() && response.body() != null) {
                                Toast.makeText(RecipeDetailActivity.this, response.body().message, Toast.LENGTH_SHORT).show();
                            }
                        }
                        @Override
                        public void onFailure(Call<PointsResponse> call, Throwable t) {}
                    });
                }
            }
            @Override
            public void onFailure(Call<ApiResponse<RecipeDto>> call, Throwable t) {}
        });
    }

    private void displayRecipe(RecipeDto recipe) {
        recipeTitle = recipe.title;
        tvTitle.setText(recipe.title);
        tvDesc.setText(recipe.description);
        tvInstructions.setText(recipe.instructions);

        StringBuilder sb = new StringBuilder();
        if (recipe.ingredients != null) {
            for (IngredientDto ing : recipe.ingredients) {
                sb.append("• ").append(ing.name)
                        .append(" - ").append(ing.quantity)
                        .append(" ").append(ing.unit != null ? ing.unit : "шт")
                        .append("\n");
            }
        }
        tvIngredients.setText(sb.toString());

        // Загружаем изображение, если оно есть
        if (recipe.imageUrl != null && !recipe.imageUrl.isEmpty()) {
            loadImage(recipe.imageUrl);
        }
    }

    private void loadImage(String imageUrl) {
        String baseUrl = "http://10.0.2.2:5286";
        String fullImageUrl = imageUrl.startsWith("http") ? imageUrl : baseUrl + imageUrl;

        Glide.with(this)
                .load(fullImageUrl)
                .diskCacheStrategy(DiskCacheStrategy.ALL)
                .placeholder(R.drawable.placeholder_image)
                .error(R.drawable.error_image)
                .into(ivImage);
    }
}