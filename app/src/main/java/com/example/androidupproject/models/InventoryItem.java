package com.example.androidupproject.models;

import com.google.gson.annotations.SerializedName;

public class InventoryItem {
    @SerializedName("id")
    public int id;

    @SerializedName("ingredientId")
    public int ingredientId;

    @SerializedName("name")
    public String name;

    @SerializedName("category")
    public String category;

    @SerializedName("unit")
    public String unit;

    @SerializedName("quantity")
    public double quantity;

    // Пустой конструктор для Retrofit
    public InventoryItem() {}
}
