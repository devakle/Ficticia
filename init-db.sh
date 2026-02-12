cd backend/src/Api.Host

dotnet tool install --global dotnet-ef --version 8.* || true

dotnet ef migrations add InitPeople \
  -c Modules.People.Infrastructure.Persistence.PeopleDbContext \
  -p ../Modules/Modules.People/Modules.People.Infrastructure/Modules.People.Infrastructure.csproj \
  -s Api.Host.csproj

dotnet ef database update \
  -c Modules.People.Infrastructure.Persistence.PeopleDbContext \
  -p ../Modules/Modules.People/Modules.People.Infrastructure/Modules.People.Infrastructure.csproj \
  -s Api.Host.csproj


dotnet ef migrations add InitIdentity \
  -c Modules.Identity.Infrastructure.Persistence.IdentityDbContext \
  -p ../Modules/Modules.Identity/Modules.Identity.Infrastructure/Modules.Identity.Infrastructure.csproj \
  -s Api.Host.csproj

dotnet ef database update \
  -c Modules.Identity.Infrastructure.Persistence.IdentityDbContext \
  -p ../Modules/Modules.Identity/Modules.Identity.Infrastructure/Modules.Identity.Infrastructure.csproj \
  -s Api.Host.csproj
