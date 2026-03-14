package com.example.androidupproject.models;

public class LoginRequest {
    private String username; // Имя должно совпадать с ожиданием сервера
    private String password;

    public LoginRequest(String username, String password) {
        this.username = username;
        this.password = password;
    }
}
