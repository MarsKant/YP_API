package com.example.androidupproject;

import android.content.Intent;
import android.os.Bundle;
import android.util.Log;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;
import androidx.appcompat.app.AppCompatActivity;

import com.example.androidupproject.models.LoginResponse;
import com.example.androidupproject.models.SessionManager;
import com.example.androidupproject.network.ApiClient;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class LoginActivity extends AppCompatActivity {
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);

        EditText etUsername = findViewById(R.id.etUsername);
        EditText etPassword = findViewById(R.id.etPassword);
        Button btnLogin = findViewById(R.id.btnLogin);

        btnLogin.setOnClickListener(v -> {
            String user = etUsername.getText().toString();
            String pass = etPassword.getText().toString();

            ApiClient.getService().login(user, pass).enqueue(new Callback<LoginResponse>() {
                @Override
                public void onResponse(Call<LoginResponse> call, Response<LoginResponse> response) {
                    Log.d("LOGIN_RESPONSE", "Code: " + response.code());

                    if (response.isSuccessful() && response.body() != null) {
                        LoginResponse loginResponse = response.body();
                        if (loginResponse.success) {
                            // Сохраняем данные пользователя
                            new SessionManager(LoginActivity.this)
                                    .saveUser(loginResponse.id, loginResponse.token);

                            startActivity(new Intent(LoginActivity.this, MainActivity.class));
                            finish();
                        } else {
                            Toast.makeText(LoginActivity.this,
                                    "Ошибка: " + loginResponse.message,
                                    Toast.LENGTH_SHORT).show();
                        }
                    } else {
                        try {
                            String errorBody = response.errorBody().string();
                            Log.e("LOGIN_ERROR", "Error body: " + errorBody);
                            Toast.makeText(LoginActivity.this,
                                    "Ошибка сервера: " + response.code(),
                                    Toast.LENGTH_SHORT).show();
                        } catch (Exception e) {
                            e.printStackTrace();
                        }
                    }
                }

                @Override
                public void onFailure(Call<LoginResponse> call, Throwable t) {
                    Log.e("NETWORK_ERROR", "Message: " + t.getMessage());
                    Toast.makeText(LoginActivity.this,
                            "Ошибка сети: " + t.getMessage(),
                            Toast.LENGTH_SHORT).show();
                }
            });
        });
    }
}