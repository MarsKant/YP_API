package com.example.androidupproject.network;

import com.google.gson.annotations.SerializedName;

public class ApiResponse<T> {
    // Ловим и "success", и "Success"
    @SerializedName(value = "success", alternate = {"Success"})
    public boolean success;

    // Ловим и "message", и "Message"
    @SerializedName(value = "message", alternate = {"Message"})
    public String message;

    // ГЛАВНОЕ: Ловим и "data", и "Data" (и "Result", на всякий случай)
    @SerializedName(value = "data", alternate = {"Data", "Result", "result"})
    public T data;
}