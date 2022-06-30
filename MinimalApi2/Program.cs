var builder = WebApplication.CreateBuilder(args);

#region Builder Services
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateActor = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuer = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromHours(6),
        ValidIssuer = builder.Configuration["JwtBearerTokenSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtBearerTokenSettings:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtBearerTokenSettings:SecretKey"]))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", p =>
    p.RequireRole("adm"));
    options.AddPolicy("Employee", p =>
    p.RequireRole("employee"));
});

builder.Services.AddSingleton<IUserService, UserService>();

#endregion

var app = builder.Build();

app.UseAuthentication(); 
app.UseAuthorization();


app.MapGet("/hi", () => "Ola mundooo").AllowAnonymous();

#region Utilization

#region Utilization Gets
app.MapGet("/utilization", async () =>
{
    using OracleConnection conn = new OracleConnection(builder.Configuration["ConnectionStrings:ConnString"]);
    conn.Open();
    var Query = await conn.QueryAsync("SELECT * FROM TB_UTILIZACAO ORDER BY CD_UTILIZACAO");

    var result = Query.ToList();
    conn.Close();

    return result.Any() == false ? Results.NotFound("Nenhuma utilização cadastrada.") : Results.Ok(result);
}).RequireAuthorization("Employee");

app.MapGet("/utilization/{code}", async (string code) =>
{
    using OracleConnection conn = new OracleConnection(builder.Configuration["ConnectionStrings:ConnString"]);

    var query = await conn.QueryAsync($"select * from TB_UTILIZACAO where CD_UTILIZACAO = {code}");

    var result = query.ToList();

    conn.Close();
    return result.Count == 0 ? Results.NotFound("Utilização não encontrada.") : Results.Ok(result);
}).RequireAuthorization("Admin");

#endregion

#region Utilization Post
app.MapPost("/utilization", async (TB_UTILIZACAO tbutil) =>
{
    OracleConnection conn = new OracleConnection(builder.Configuration["ConnectionStrings:ConnString"]);

    var util = new TB_UTILIZACAO(tbutil.CD_UTILIZACAO, tbutil.TX_DESCRICAO)
    {
        CD_UTILIZACAO = tbutil.CD_UTILIZACAO,
        TX_DESCRICAO = tbutil.TX_DESCRICAO
    };

    if (!util.IsValid)
    {
        return Results.ValidationProblem(util.Notifications.ConvertToProblemDetails());
    }

    var query = await conn.QueryAsync($"select * from TB_UTILIZACAO where CD_UTILIZACAO = {util.CD_UTILIZACAO}");
    var validation = query.ToList();
    if (validation.Count == 0)
    {
        var result = await conn.ExecuteAsync($"INSERT INTO TB_UTILIZACAO (CD_UTILIZACAO,TX_DESCRICAO) VALUES ('{util.CD_UTILIZACAO}','{util.TX_DESCRICAO}')");
        return Results.Ok("Utilização adicionada com sucesso!");
    }
    conn.Close();
    return Results.BadRequest("Código de utilização já existe.");
}).RequireAuthorization("Admin");
#endregion

#region Utilization Put
app.MapPut("/utilization/{id}", async ([FromRoute] string id, TB_UTILIZACAO tbutil) => //bu code
{
    if (string.IsNullOrEmpty(id))
        return Results.BadRequest("Código obrigatório.");

    OracleConnection conn = new OracleConnection(builder.Configuration["ConnectionStrings:ConnString"]);

    var util = new TB_UTILIZACAO(id, tbutil.TX_DESCRICAO)
    {
        CD_UTILIZACAO = id,
        TX_DESCRICAO = tbutil.TX_DESCRICAO
    };

    if (!util.IsValid)
    {
        return Results.ValidationProblem(util.Notifications.ConvertToProblemDetails());
    }

    var result = await conn.ExecuteAsync($"UPDATE TB_UTILIZACAO SET TX_DESCRICAO = '{util.TX_DESCRICAO}' WHERE CD_UTILIZACAO = '{util.CD_UTILIZACAO}'");

    conn.Close();
    return result == 0 ? Results.BadRequest("Utilização não encontrada.") : Results.Ok("Utilização alterada com sucesso!");
}).RequireAuthorization("Employee");
#endregion

#region Utilization Delete
app.MapDelete("/utilization/{id}", async (string id) =>
{
    OracleConnection conn = new OracleConnection(builder.Configuration["ConnectionStrings:ConnString"]);
    var result = await conn.ExecuteAsync($"DELETE FROM TB_UTILIZACAO WHERE CD_UTILIZACAO = {id}");

    if (result == 0)
        return Results.NotFound("Utilização não encontrada.");

    conn.Close();
    return result == 0 ? Results.NotFound("Utilização não encontrada.") : Results.Ok("Utilização excluída com sucesso!");
}).RequireAuthorization("Admin");
#endregion

#endregion

#region Login
app.MapPost("/login", (UserLogin user, IUserService service) =>
{
    if (!string.IsNullOrEmpty(user.Name) &&
        !string.IsNullOrEmpty(user.Password))
    {
        var usuariologado = service.Get(user);

        if (usuariologado is null) return Results.NotFound("Usuário não encontrado.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuariologado.Name),
            new Claim(ClaimTypes.Role, usuariologado.Role)
        };

        var token = new JwtSecurityToken(
            issuer: builder.Configuration["JwtBearerTokenSettings:Issuer"],
            audience: builder.Configuration["JwtBearerTokenSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(6),
            notBefore: DateTime.UtcNow,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtBearerTokenSettings:SecretKey"])),
                SecurityAlgorithms.HmacSha256)
            );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Results.Ok(tokenString);
    }
    else
        return Results.BadRequest("Credenciais de usuário inválidas");
}).AllowAnonymous();
#endregion


app.Run();

