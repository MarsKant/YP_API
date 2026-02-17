package com.example.androidupproject.models;
import com.google.gson.annotations.SerializedName;

public class MenuItemDto {
    @SerializedName(value = "RecipeId", alternate = {"recipeId"})
    public int recipeId;

    @SerializedName(value = "RecipeTitle", alternate = {"recipeTitle", "title", "Title"})
    public String recipeTitle;

    @SerializedName(value = "Date", alternate = {"date"})
    public String date;

    @SerializedName(value = "MealType", alternate = {"mealType"})
    public String mealType;
}