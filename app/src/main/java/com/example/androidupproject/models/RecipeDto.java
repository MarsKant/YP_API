package com.example.androidupproject.models;

import com.google.gson.annotations.SerializedName;
import java.util.List;

public class RecipeDto {
    @SerializedName(value = "Id", alternate = {"id"})
    public int id;

    @SerializedName(value = "Title", alternate = {"title"})
    public String title;

    @SerializedName(value = "Description", alternate = {"description"})
    public String description;

    @SerializedName(value = "Instructions", alternate = {"instructions"})
    public String instructions;

    @SerializedName(value = "ImageUrl", alternate = {"imageUrl", "Image", "image"})
    public String imageUrl;

    @SerializedName(value = "Calories", alternate = {"calories"})
    public double calories;

    @SerializedName(value = "Ingredients", alternate = {"ingredients"})
    public List<IngredientDto> ingredients;
}