package com.example.androidupproject.network;

import com.google.gson.annotations.SerializedName;

public class ApiResponse<T> {
    public boolean success;
    public String message;
    public T data;
}