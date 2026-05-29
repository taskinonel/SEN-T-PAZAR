# Android API Guide

This document provides a ready-to-use Retrofit surface for the SEN-T PAZAR backend.

## Base URLs

Use one of these depending on your environment:

- Development: `http://10.0.2.2:5080`
- Production: `https://your-domain.com`

## Authentication

- JWT-protected endpoints require `Authorization: Bearer <token>`.
- The `/api/listings` management endpoints require `X-API-Key` and are protected by the API key middleware.
- Public browse endpoints under `/api/v1/Ads` and metadata endpoints under `/api/v1/Categories` / `/api/v1/Locations` are the main Android-friendly surfaces.

## Retrofit models

```kotlin
data class LoginRequest(
    val userNameOrEmail: String,
    val password: String
)

data class RegisterRequest(
    val fullName: String,
    val email: String,
    val userName: String,
    val password: String
)

data class AuthResponse(
    val token: String,
    val expiresAtUtc: String,
    val user: MobileUser
)

data class MobileUser(
    val id: String,
    val userName: String,
    val email: String,
    val fullName: String
)

data class AdListItem(
    val id: Int,
    val title: String,
    val price: Double,
    val location: String,
    val imageUrl: String,
    val isSponsored: Boolean
)

data class AdDetails(
    val id: Int,
    val title: String,
    val description: String,
    val price: Double,
    val priceCurrency: String,
    val category: String,
    val listingType: String,
    val location: String,
    val isSponsored: Boolean,
    val imageUrls: List<String>,
    val seller: AdSeller
)

data class AdSeller(
    val name: String,
    val phone: String,
    val allowWhatsApp: Boolean,
    val allowMessages: Boolean
)

data class CategoryDto(
    val code: String,
    val name: String,
    val slug: String
)

data class LocationDto(
    val city: String,
    val districts: List<String>
)

data class DeviceTokenRequest(
    val fcmToken: String
)

data class UploadResponse(
    val urls: List<String>
)
```

## Retrofit service

```kotlin
import okhttp3.MultipartBody
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.DELETE
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.Part
import retrofit2.http.Path
import retrofit2.http.Query

interface SenTApiService {

    @POST("api/v1/Account/Login")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>

    @POST("api/v1/Account/Register")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>

    @POST("api/v1/Account/DeviceToken")
    suspend fun saveDeviceToken(@Body request: DeviceTokenRequest): Response<Unit>

    @GET("api/v1/Account/Profile")
    suspend fun profile(): Response<Any>

    @GET("api/v1/Account/MyAds")
    suspend fun myAds(): Response<List<Any>>

    @GET("api/v1/Account/Messages")
    suspend fun messages(): Response<List<Any>>

    @POST("api/v1/Account/Messages/Reply")
    suspend fun replyMessage(@Body body: Map<String, Any>): Response<Any>

    @GET("api/v1/Ads")
    suspend fun getAds(
        @Query("category") category: String? = null,
        @Query("query") query: String? = null,
        @Query("listingType") listingType: String? = null,
        @Query("minPrice") minPrice: Double? = null,
        @Query("maxPrice") maxPrice: Double? = null,
        @Query("page") page: Int = 1
    ): Response<List<AdListItem>>

    @GET("api/v1/Ads/{id}")
    suspend fun getAdDetails(@Path("id") id: Int): Response<AdDetails>

    @GET("api/v1/Ads/Suggest")
    suspend fun suggest(@Query("q") query: String): Response<List<String>>

    @POST("api/v1/Ads")
    suspend fun createAd(@Body body: Map<String, Any?>): Response<Map<String, Any>>

    @Multipart
    @POST("api/v1/Ads/Upload")
    suspend fun uploadImages(@Part files: List<MultipartBody.Part>): Response<UploadResponse>

    @DELETE("api/v1/Ads/{id}")
    suspend fun deleteAd(@Path("id") id: Int): Response<Unit>

    @GET("api/v1/Categories")
    suspend fun getCategories(): Response<List<CategoryDto>>

    @GET("api/v1/Locations")
    suspend fun getLocations(): Response<List<LocationDto>>
}
```

## OkHttp setup

```kotlin
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory

fun createApiService(baseUrl: String, tokenProvider: () -> String?): SenTApiService {
    val logging = HttpLoggingInterceptor().apply {
        level = HttpLoggingInterceptor.Level.BODY
    }

    val client = OkHttpClient.Builder()
        .addInterceptor { chain ->
            val original = chain.request()
            val builder = original.newBuilder()

            tokenProvider()?.takeIf { it.isNotBlank() }?.let { token ->
                builder.header("Authorization", "Bearer $token")
            }

            builder.header("User-Agent", "SENTPAZAR-Android-App")
            chain.proceed(builder.build())
        }
        .addInterceptor(logging)
        .build()

    return Retrofit.Builder()
        .baseUrl(baseUrl)
        .client(client)
        .addConverterFactory(GsonConverterFactory.create())
        .build()
        .create(SenTApiService::class.java)
}
```

## Example calls

### Login

```kotlin
val response = api.login(LoginRequest("user@example.com", "123456"))
```

### Fetch listings

```kotlin
val response = api.getAds(category = "estate", query = "daire", page = 1)
```

### Fetch categories

```kotlin
val response = api.getCategories()
```

## Notes

- For category and listing browse screens, the Android app should prefer `/api/v1/Ads`, `/api/v1/Ads/{id}`, `/api/v1/Categories`, and `/api/v1/Locations`.
- If you need admin-style listing management with API keys, use the `/api/listings` endpoints separately.
- The backend returns localized titles/descriptions on mobile ad endpoints using the `Accept-Language` header.
