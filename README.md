# SIGOB SSO Identity Provider

Proveedor central de identidad para los sistemas SIGOB, construido con ASP.NET Core Identity y
OpenIddict. Implementa OAuth 2.0 y OpenID Connect para autenticar usuarios, emitir tokens, exponer
información del perfil y finalizar sesiones de forma centralizada.

Esta guía es la documentación oficial de consumo de la API. En Development también se publica como
descripción del documento OpenAPI mostrado por Scalar:

- Scalar: `https://localhost:7293/scalar`
- OpenAPI: `https://localhost:7293/openapi/v1.json`
- Issuer local: `https://localhost:7293/`

> Los clientes deben descubrir las direcciones reales mediante
> `/.well-known/openid-configuration`. No deben construir manualmente las URL del protocolo.

## Capacidades y flujos soportados

| Tipo de cliente                        | Flujo                     | Autenticación del cliente    | Recomendación |
| -------------------------------------- | ------------------------- | ---------------------------- | ------------- |
| SPA (Blazor WASM, React, Angular, Vue) | Authorization Code + PKCE | Cliente público, sin secreto | Recomendado   |
| Web server-side / BFF                  | Authorization Code + PKCE | Cliente confidencial         | Recomendado   |
| Aplicación nativa, móvil o desktop     | Authorization Code + PKCE | Cliente público, sin secreto | Recomendado   |
| Servicio M2M / daemon / worker         | Client Credentials        | Cliente confidencial         | Recomendado   |
| Renovación de sesión                   | Refresh Token             | Según el tipo de cliente     | Soportado     |

No se soportan los flujos `implicit`, Resource Owner Password (`password`) ni Device Authorization.
PKCE es obligatorio para Authorization Code y únicamente se acepta `S256`.

## Registro previo de clientes

Todo consumidor debe registrarse antes de iniciar un flujo. El registro define:

- `client_id` y, para clientes confidenciales, `client_secret`.
- Tipo de cliente: público o confidencial.
- `redirect_uri` exactas permitidas.
- `post_logout_redirect_uri` exactas permitidas.
- Flujos y scopes autorizados.
- Tipo de consentimiento: `Explicit`, `Implicit`, `External` o `Systematic`.

Las URI se comparan exactamente. Diferencias en esquema, host, puerto, ruta o barra final producen
`invalid_request` o `invalid_client`. Un secreto nunca debe incluirse en una SPA, aplicación móvil o
binario distribuido al usuario.

## Discovery y claves públicas

### `GET /.well-known/openid-configuration`

Documento de descubrimiento OIDC generado por OpenIddict. Informa el issuer, endpoints, grants,
scopes, algoritmos y métodos PKCE soportados.

```bash
curl https://localhost:7293/.well-known/openid-configuration
```

Los SDK OIDC deben configurarse con la autoridad `https://localhost:7293/` para que consuman este
documento automáticamente.

### `GET /.well-known/jwks`

Publica las claves de firma necesarias para validar localmente los JWT. Los resource servers deben
usar discovery/JWKS y respetar la rotación de claves; no deben copiar una clave pública estática a su
configuración.

```bash
curl https://localhost:7293/.well-known/jwks
```

## Endpoints de interacción

### `GET|POST /connect/authorize`

Punto de entrada interactivo. El cliente redirige aquí el navegador; el proveedor valida al cliente,
la sesión del usuario, PKCE, scopes, `prompt`, `max_age` y consentimiento. Nunca debe invocarse con
AJAX ni desde un backend sin navegador.

Parámetros principales:

| Parámetro               | Obligatorio | Descripción                                                  |
| ----------------------- | ----------- | ------------------------------------------------------------ |
| `client_id`             | Sí          | Identificador del cliente registrado                         |
| `response_type`         | Sí          | Debe ser `code`                                              |
| `redirect_uri`          | Sí          | URI registrada a la que se devolverá el código               |
| `scope`                 | Sí          | Lista separada por espacios; para OIDC debe incluir `openid` |
| `code_challenge`        | Sí          | Desafío PKCE derivado del verifier                           |
| `code_challenge_method` | Sí          | Debe ser `S256`                                              |
| `state`                 | Sí          | Valor aleatorio del cliente para prevenir CSRF               |
| `nonce`                 | Recomendado | Vincula el ID token con la solicitud original                |
| `prompt`                | No          | `none`, `login`, `consent` o `select_account`                |
| `max_age`               | No          | Antigüedad máxima de la autenticación, en segundos           |

Ejemplo conceptual (la URL debe abrirse mediante navegación del navegador):

```text
https://localhost:7293/connect/authorize
	?client_id=blazor-spa-client
	&response_type=code
	&redirect_uri=https%3A%2F%2Flocalhost%3A7026%2Fauthentication%2Flogin-callback
	&scope=openid%20profile%20email%20roles%20offline_access%20sso%3Aadmin
	&code_challenge=<BASE64URL_SHA256_CODE_VERIFIER>
	&code_challenge_method=S256
	&state=<RANDOM_VALUE>
	&nonce=<RANDOM_VALUE>
```

Resultado: redirección a `redirect_uri` con `code`, `state` e `iss`. Con `prompt=none`, si se necesita
login o consentimiento, devuelve `login_required` o `consent_required` a la `redirect_uri`.

### `POST /connect/token`

Intercambia credenciales válidas por tokens. Consume
`application/x-www-form-urlencoded`; no acepta JSON. El tipo y contenido de los tokens dependen del
grant y scopes concedidos.

#### Authorization Code + PKCE

```bash
curl -X POST https://localhost:7293/connect/token \
	-H 'Content-Type: application/x-www-form-urlencoded' \
	--data-urlencode 'grant_type=authorization_code' \
	--data-urlencode 'client_id=blazor-spa-client' \
	--data-urlencode 'code=<AUTHORIZATION_CODE>' \
	--data-urlencode 'redirect_uri=https://localhost:7026/authentication/login-callback' \
	--data-urlencode 'code_verifier=<ORIGINAL_CODE_VERIFIER>'
```

#### Refresh Token

```bash
curl -X POST https://localhost:7293/connect/token \
	-H 'Content-Type: application/x-www-form-urlencoded' \
	--data-urlencode 'grant_type=refresh_token' \
	--data-urlencode 'client_id=blazor-spa-client' \
	--data-urlencode 'refresh_token=<REFRESH_TOKEN>'
```

El usuario se revalida en cada renovación. Si fue eliminado o desactivado, la respuesta es
`invalid_grant`, aunque el refresh token todavía no haya expirado.

#### Client Credentials (M2M)

```bash
curl -X POST https://localhost:7293/connect/token \
	-u 'sigob-m2m-client:<CLIENT_SECRET>' \
	-H 'Content-Type: application/x-www-form-urlencoded' \
	--data-urlencode 'grant_type=client_credentials' \
	--data-urlencode 'scope=sso:admin'
```

También puede autenticarse con `client_secret_post` si el SDK lo requiere, aunque
`client_secret_basic` es preferible. El token M2M representa al cliente, no a una persona: no existe
ID token ni UserInfo de usuario.

Respuesta típica:

```json
{
	"access_token": "<JWT>",
	"token_type": "Bearer",
	"expires_in": 3600,
	"scope": "openid profile email roles offline_access",
	"id_token": "<JWT>",
	"refresh_token": "<TOKEN>"
}
```

### `GET|POST /connect/userinfo`

Devuelve claims actuales del usuario representado por el access token. Requiere
`Authorization: Bearer <access_token>`. Los claims se filtran según los scopes concedidos.

```bash
curl https://localhost:7293/connect/userinfo \
	-H 'Authorization: Bearer <ACCESS_TOKEN>'
```

```json
{
	"sub": "c2d37664-9d4c-48bc-82e3-52aebd2decdc",
	"name": "Admin SIGOB",
	"preferred_username": "admin@sigob.org",
	"email": "admin@sigob.org",
	"email_verified": true,
	"role": ["Admin"],
	"tenant_id": "11111111-1111-1111-1111-111111111111"
}
```

No debe usarse con tokens M2M. Una cuenta eliminada o desactivada produce `invalid_token`.

### `GET|POST /connect/logout`

Implementa RP-Initiated Logout. `GET` muestra confirmación; `POST` elimina la cookie central y deja
que OpenIddict valide la redirección posterior.

Parámetros:

| Parámetro                  | Descripción                                  |
| -------------------------- | -------------------------------------------- |
| `id_token_hint`            | ID token de la sesión que se desea finalizar |
| `post_logout_redirect_uri` | URI registrada para regresar al cliente      |
| `state`                    | Estado opaco que se devuelve al cliente      |

```text
https://localhost:7293/connect/logout
	?id_token_hint=<ID_TOKEN>
	&post_logout_redirect_uri=https%3A%2F%2Flocalhost%3A7026%2Fauthentication%2Flogout-callback
	&state=<RANDOM_VALUE>
```

La aplicación debe navegar a esta URL, no hacer logout mediante una llamada AJAX. Una
`post_logout_redirect_uri` no registrada es rechazada.

## Health checks

| Endpoint            | Uso                                                         | Respuesta saludable                   |
| ------------------- | ----------------------------------------------------------- | ------------------------------------- |
| `GET /health/live`  | Confirma que el proceso está vivo; no consulta dependencias | `200 Healthy`                         |
| `GET /health/ready` | Confirma que la base de identidad es accesible              | `200 Healthy`; `503` si no está lista |

## Scopes y claims

| Scope            | Claims habilitados                                                                |
| ---------------- | --------------------------------------------------------------------------------- |
| `openid`         | Activa OIDC e ID token; `sub`                                                     |
| `profile`        | `name`, `given_name`, `family_name`, `preferred_username`, `birthdate`, `picture` |
| `email`          | `email`, `email_verified`                                                         |
| `roles`          | `role`                                                                            |
| `offline_access` | Habilita refresh token                                                            |
| `sso:admin`      | Autoriza operaciones administrativas del recurso SSO                              |

El access token no contiene PII. Solo transporta identidad técnica y autorización (`sub`,
`client_id`, `scope`, `tenant_id`, `role`, audiencia y metadatos del token). Los datos personales se
obtienen del ID token o UserInfo según los scopes concedidos. Los clientes no deben depender de la
estructura interna del access token; deben tratarlo como opaco.

## Consumo por tipo de cliente

### SPA

1. Registrar como cliente público, sin secreto.
2. Generar un `code_verifier` criptográficamente aleatorio por operación.
3. Calcular `code_challenge = BASE64URL(SHA256(code_verifier))`.
4. Navegar a `/connect/authorize`, validando `state` y `nonce` al regresar.
5. Intercambiar el código en `/connect/token` con el verifier original.
6. Mantener los tokens en memoria cuando sea posible; evitar `localStorage` por exposición a XSS.
7. Renovar con refresh token y navegar a `/connect/logout` al finalizar.

Para Blazor WebAssembly usar `Microsoft.AspNetCore.Components.WebAssembly.Authentication`; para
React/Angular/Vue usar una biblioteca OIDC certificada. No implementar PKCE ni validación de tokens
manualmente.

Configuración conceptual:

```json
{
	"authority": "https://localhost:7293/",
	"client_id": "blazor-spa-client",
	"redirect_uri": "https://localhost:7026/authentication/login-callback",
	"post_logout_redirect_uri": "https://localhost:7026/authentication/logout-callback",
	"response_type": "code",
	"scope": "openid profile email roles offline_access sso:admin"
}
```

### Aplicación web server-side o BFF

1. Registrar como cliente confidencial con secreto o autenticación por clave privada.
2. Configurar middleware OpenID Connect con `response_type=code`, PKCE y cookie local segura.
3. Guardar secretos y tokens únicamente en el servidor; el navegador recibe una cookie `HttpOnly`.
4. Validar antiforgery en operaciones del BFF.
5. Usar `SaveTokens` solo si realmente se necesitan; preferir un almacén server-side de tokens.

Ejemplo ASP.NET Core:

```csharp
services.AddAuthentication(options =>
{
		options.DefaultScheme = "app-cookie";
		options.DefaultChallengeScheme = "oidc";
})
.AddCookie("app-cookie")
.AddOpenIdConnect("oidc", options =>
{
		options.Authority = "https://localhost:7293/";
		options.ClientId = "sigob-web";
		options.ClientSecret = configuration["Oidc:ClientSecret"];
		options.ResponseType = "code";
		options.UsePkce = true;
		options.Scope.Add("openid");
		options.Scope.Add("profile");
		options.Scope.Add("email");
		options.Scope.Add("roles");
		options.Scope.Add("offline_access");
});
```

### Aplicación móvil, desktop o CLI interactiva

Usar cliente público con Authorization Code + PKCE y abrir el navegador del sistema. La URI de
retorno debe ser un universal/app link HTTPS o loopback URI registrada. No usar WebView embebida ni
incluir secretos en el ejecutable. Device Flow no está habilitado; una CLI sin navegador no puede
autenticar usuarios en esta etapa.

### Servicio M2M, daemon o background worker

1. Registrar como cliente confidencial con permiso `client_credentials`.
2. Guardar el secreto en un vault o variable segura, nunca en el repositorio.
3. Solicitar únicamente scopes de recurso, por ejemplo `sso:admin`; no solicitar `openid`, `profile`,
	 `email`, `roles` ni `offline_access`.
4. Cachear el access token hasta poco antes de `expires_in`; no pedir uno por cada llamada.
5. Enviar el token al resource server como `Authorization: Bearer <token>`.

No existe usuario ni consentimiento interactivo en este flujo.

### Resource server / API protegida

La API receptora valida issuer, audience/resource, firma, expiración y scopes/roles. Debe cargar la
configuración desde discovery/JWKS. Mientras el access token sea JWT legible puede validarlo
localmente; si se habilita cifrado posteriormente deberá compartir la clave de descifrado o usar
introspección. En ningún caso debe aceptar solo que el JWT “se pueda decodificar”.

## Errores OAuth/OIDC

Los endpoints `/connect/*` devuelven errores estándar:

| Error                    | Causa habitual                                                         |
| ------------------------ | ---------------------------------------------------------------------- |
| `invalid_request`        | Parámetro obligatorio ausente o combinación inválida                   |
| `invalid_client`         | Cliente desconocido, secreto incorrecto o método no permitido          |
| `invalid_grant`          | Código/refresh token inválido, reutilizado o usuario desactivado       |
| `invalid_scope`          | Scope no registrado o no autorizado al cliente                         |
| `unsupported_grant_type` | Grant no habilitado                                                    |
| `login_required`         | `prompt=none` pero no existe sesión válida                             |
| `consent_required`       | `prompt=none` pero se necesita consentimiento                          |
| `access_denied`          | El usuario rechazó el consentimiento o su cuenta está deshabilitada    |
| `invalid_token`          | Bearer ausente, expirado, inválido o asociado a una cuenta inexistente |

Ejemplo:

```json
{
	"error": "invalid_grant",
	"error_description": "The token is no longer valid because the account is disabled or no longer exists."
}
```

No se debe mostrar `error_description` directamente al usuario final ni usarlo para lógica; la
integración debe decidir por el valor estable de `error`.

## Reglas de seguridad para integradores

- Usar HTTPS en todos los entornos compartidos y validar certificados.
- Validar `state`, `nonce`, issuer, audience, firma y expiración.
- No registrar authorization codes, secretos, access tokens, ID tokens ni refresh tokens.
- No enviar tokens en query strings.
- Tratar el access token como opaco, aunque actualmente sea un JWT legible.
- Solicitar el conjunto mínimo de scopes.
- No compartir tokens entre clientes ni reutilizar códigos de autorización.
- Ante `401`, intentar como máximo una renovación coordinada; evitar bucles de refresh.
- En logout, limpiar también la sesión local del cliente.

## Desarrollo local

```bash
dotnet run --project pres/api/Identity.Sso.Api/Identity.Sso.Api.csproj --launch-profile https
```

La configuración sensible se suministra mediante `.env` no versionado o variables de entorno. Ver
`../.env.example`. Los certificados de desarrollo y la relajación del requisito de transporte solo
se habilitan fuera de Production.