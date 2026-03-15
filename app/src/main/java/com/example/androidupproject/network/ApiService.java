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

    // === ИНВЕНТАРЬ (ХОЛОДИЛЬНИК) ===
    @GET("api/Inventory/fridge/{userId}")
    Call<InventoryResponse> getUserInventory(@Path("userId") int userId);

    @POST("api/Inventory/add/{userId}")
    Call<SimpleResponse> addToInventory(
            @Path("userId") int userId,
            @Body InventoryAddRequest request
    );

    @DELETE("api/Inventory/user/{userId}/ingredient/{ingredientId}")
    Call<SimpleResponse> deleteInventoryItem(
            @Path("userId") int userId,
            @Path("ingredientId") int ingredientId
    );

    // === МЕНЮ - ИСПРАВЛЕННЫЕ ЭНДПОИНТЫ ===

    // Получение всех меню пользователя (возвращает массив MenuDto)
    @GET("api/Menu/user/{userId}/all")
    Call<List<MenuDto>> getUserMenus(@Path("userId") int userId);

    // Получение деталей конкретного меню
    @GET("api/Menu/{menuId}")
    Call<MenuDetailsResponse> getMenuDetails(@Path("menuId") int menuId);

    // Генерация нового меню
    @POST("api/Menu/generate-week/{userId}")
    Call<GenerateMenuResponse> generateMenu(@Path("userId") int userId);

    // Удаление меню
    @DELETE("api/Menu/{menuId}")
    Call<SimpleResponse> deleteMenu(@Path("menuId") int menuId);

    // === ИЗБРАННОЕ ===
    @GET("api/Recipes/favorites/{userId}")
    Call<ApiResponse<List<RecipeDto>>> getFavorites(@Path("userId") int userId);

    @POST("api/Recipes/{recipeId}/favorite/{userId}")
    Call<ApiResponse<Void>> toggleFavorite(@Path("recipeId") int recipeId, @Path("userId") int userId);

    // === РЕЦЕПТЫ ===
    @GET("api/Recipes/{id}")
    Call<ApiResponse<RecipeDto>> getRecipeById(@Path("id") int id);

    // === СПИСОК ПОКУПОК ===
    @GET("api/ShoppingList/user/{userId}/current")
    Call<ApiResponse<ShoppingListDto>> getShoppingList(@Path("userId") int userId);

    @POST("api/ShoppingList/generate-from-menu/{menuId}/{userId}")
    Call<ApiResponse<Void>> generateShoppingList(@Path("menuId") int menuId, @Path("userId") int userId);

    @PUT("api/ShoppingList/items/{itemId}/toggle")
    Call<ApiResponse<Void>> toggleShoppingItem(@Path("itemId") int itemId);

    @DELETE("api/ShoppingList/items/{itemId}")
    Call<ApiResponse<Void>> deleteShoppingItem(@Path("itemId") int itemId);

    // === ГЕНЕРАЦИЯ КАРТИНОК ===
    @POST("api/Images/generate")
    Call<ImageGenerationResponse> generateRecipeImage(@Body ImageGenerationRequest request);

    // Парсинг изображения с Povar.ru
    @GET("api/Images/parse-povar")
    Call<ImageParseResponse> parsePovarImage(@Query("query") String query, @Query("RecipeId") int recipeId);
}