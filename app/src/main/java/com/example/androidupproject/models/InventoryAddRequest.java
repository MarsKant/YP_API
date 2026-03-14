package com.example.androidupproject.models;

public class InventoryAddRequest {
    public String productName;
    public double quantity;
    public String unit;

    public InventoryAddRequest(String productName, double quantity, String unit) {
        this.productName = productName;
        this.quantity = quantity;
        this.unit = unit;
    }
}
