package com.example.androidupproject.network;

import com.example.androidupproject.models.*;
import java.util.List;
import retrofit2.Call;
import retrofit2.http.*;

public interface ApiService {
    // === АВТОРИЗАЦИЯ ===
    @FormUrlEncoded
    @POST("api/Auth/login")
    Call<LoginResponse> login(
            @Field("username") String username,
            @Field("password") String password
    );

    @FormUrlEncoded
    @POST("api/Auth/register")
    Call<LoginResponse> register(
            @Field("username") String username,
            @Field("password") String password,
            @Field("email") String email
    );

    // === РЕЦЕПТЫ ===
    @GET("api/Recipes")
    Call<ApiResponse<List<RecipeDto>>> getRecipes();

    @GET("api/Recipes/{id}")
    Call<ApiResponse<RecipeDto>> getRecipeById(@Path("id") int id);

    // === ИНВЕНТАРЬ (ХОЛОДИЛЬНИК) ===

    // Получение списка (маршрут из вашего InventoryController)
    @GET("api/Inventory/fridge/{userId}")
    Call<InventoryResponse> getUserInventory(@Path("userId") int userId);

    // Добавление через JSON
    @POST("api/Inventory/add/{userId}")
    Call<SimpleResponse> addToInventory(
            @Path("userId") int userId,
            @Body InventoryAddRequest request
    );

    // Удаление (Используем этот метод для MainActivity)
    // В контроллере: [HttpDelete("user/{userId}/ingredient/{ingredientId}")]
    @DELETE("api/Inventory/user/{userId}/ingredient/{ingredientId}")
    Call<SimpleResponse> deleteInventoryItem(
            @Path("userId") int userId,
            @Path("ingredientId") int ingredientId
    );

    // === МЕНЮ ===
    @GET("api/Menu/user/{userId}")
    Call<ApiResponse<List<MenuDto>>> getUserMenu(@Path("userId") int userId);

    @POST("api/Menu/generate-week/{userId}")
    Call<SimpleResponse> generateMenu(@Path("userId") int userId);

    @DELETE("api/Menu/{menuId}")
    Call<ApiResponse<Void>> deleteMenu(@Path("menuId") int id);

    // === ИЗБРАННОЕ ===
    @GET("api/Recipes/favorites/{userId}")
    Call<ApiResponse<List<RecipeDto>>> getFavorites(@Path("userId") int userId);

    @POST("api/Recipes/{recipeId}/favorite/{userId}")
    Call<ApiResponse<Void>> toggleFavorite(@Path("recipeId") int recipeId, @Path("userId") int userId);

    // === СПИСОК ПОКУПОК ===
    @GET("api/ShoppingList/user/{userId}/current")
    Call<ApiResponse<ShoppingListDto>> getShoppingList(@Path("userId") int userId);

    @POST("api/ShoppingList/generate-from-menu/{menuId}/{userId}")
    Call<ApiResponse<Void>> generateShoppingList(@Path("menuId") int menuId, @Path("userId") int userId);

    // Метод для переключения состояния (куплено/не куплено)
    @PUT("api/ShoppingList/items/{itemId}/toggle")
    Call<ApiResponse<Void>> toggleShoppingItem(@Path("itemId") int itemId);

    // Метод для удаления одного пункта из списка
    @DELETE("api/ShoppingList/items/{itemId}")
    Call<ApiResponse<Void>> deleteShoppingItem(@Path("itemId") int itemId);
}