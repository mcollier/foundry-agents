# froyo-agent

Froyo Fin, a frozen yogurt agent for Froyo Foundry, hosted as an
ASP.NET minimal API in `/source/froyo-agent`.

## Run locally

```bash
cd source/froyo-agent
dotnet restore
dotnet run
```

The default endpoint is:

```text
GET http://localhost:5000/
```

If ASP.NET selects a different local port, use the URL shown by `dotnet run`.
