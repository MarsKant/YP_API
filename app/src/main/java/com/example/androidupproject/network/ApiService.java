package com.example.androidupproject.network;

import com.example.androidupproject.models.IngredientDto;
import com.example.androidupproject.models.LoginResponse;
import com.example.androidupproject.models.MenuDto;
import com.example.androidupproject.models.RecipeDto;
import com.example.androidupproject.models.ShoppingListDto;

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
            @Field("email") String email,
            @Field("password") String password
    );

    // === ХОЛОДИЛЬНИК ===

    @GET("api/Ingredients/fridge/{userId}")
    Call<ApiResponse<List<IngredientDto>>> getFridge(@Path("userId") int userId);

    @POST("api/Inventory/FridgeItem/add/{userId}")
    Call<ApiResponse<Void>> addToFridge(
            @Path("userId") int userId,
            @Body IngredientDto ingredient
    );

    @DELETE("/api/Inventory/user/{userId}/ingredient/{ingredientId}")
    Call<ApiResponse<Void>> removeFromFridge(
            @Path("userId") int userId,
            @Path("ingredientId") int ingredientId
    );

    // === МЕНЮ И РЕЦЕПТЫ ===

    @GET("api/menu/user/{userId}/current")
    Call<ApiResponse<MenuDto>> getCurrentMenu(@Path("userId") int userId);

    @GET("api/recipes/{id}")
    Call<ApiResponse<RecipeDto>> getRecipe(@Path("id") int id);

    @POST("/api/Ai/ask/{userId}")
    Call<ApiResponse<Void>> generateMenu(@Path("userId") int userId, @Body List<IngredientDto> ingredientDtoList);

    // === ИЗБРАННОЕ ===

    @GET("api/recipes/favorites/{userId}")
    Call<ApiResponse<List<RecipeDto>>> getFavorites(@Path("userId") int userId);

    @POST("api/recipes/{recipeId}/favorite/{userId}")
    Call<ApiResponse<Void>> toggleFavorite(@Path("recipeId") int recipeId, @Path("userId") int userId);

    // === СПИСОК ПОКУПОК ===

    @POST("api/shoppinglist/generate-from-menu/{menuId}/{userId}")
    Call<ApiResponse<Void>> generateShoppingList(@Path("menuId") int menuId, @Path("userId") int userId);

    @GET("api/shoppinglist/user/{userId}/current")
    Call<ApiResponse<ShoppingListDto>> getShoppingList(@Path("userId") int userId);

    @PUT("api/shoppinglist/items/{itemId}/toggle")
    Call<ApiResponse<Void>> toggleShoppingItem(@Path("itemId") int itemId);
}