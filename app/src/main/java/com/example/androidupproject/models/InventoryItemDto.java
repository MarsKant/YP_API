package com.example.androidupproject.models;
import com.google.gson.annotations.SerializedName;

public class InventoryItemDto {
    @SerializedName("id")
    public int id;

    @SerializedName("ingredientId")
    public int ingredientId;

    @SerializedName("name")
    public String name;

    @SerializedName("quantity")
    public double quantity;

    @SerializedName("unit")
    public String unit;

    @SerializedName("category")
    public String category;
}