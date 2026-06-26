# Expense Tracker API

A small **ASP.NET Core 9 Web API** for tracking expenses — built to practice **.NET Core**,
**SQL (EF Core)**, and a full **CI/CD → Docker → ECR → EKS → RDS** deployment on AWS.

## What it does
- **Users / Categories / Expenses** with one-to-many relationships (EF Core Code-First)
- CRUD for expenses & categories; reports with `GROUP BY` / `SUM` aggregates
- Swagger UI, health checks (`/health/live`, `/health/ready`), auto-migrate on startup

## Tech
ASP.NET Core 9 · EF Core (SQL Server) · xUnit · Docker · Kubernetes (EKS) · GitHub Actions

---

## Project layout
```
ExpenseTracker.sln
├─ ExpenseTracker.Api/         # the Web API
│  ├─ Models/                  # User, Category, Expense  -> DB tables
│  ├─ Data/AppDbContext.cs     # relationships, indexes, seed data
│  ├─ Dtos/                    # request/response shapes
│  ├─ Controllers/             # Expenses, Categories, Reports
│  ├─ Migrations/              # generated SQL (InitialCreate)
│  ├─ Program.cs               # DI, EF Core, Swagger, health checks
│  └─ Dockerfile
├─ ExpenseTracker.Tests/       # xUnit tests (CI gate)
├─ k8s/                        # namespace, secret template, deployment, service, ingress
└─ .github/workflows/ci-cd.yml # build -> test -> ECR -> EKS
```

---

## 1. Run locally

Requires .NET 9 SDK and SQL Server. Set the connection string in
`ExpenseTracker.Api/appsettings.json` (`ConnectionStrings:DefaultConnection`).

```powershell
cd ExpenseTracker.Api

# Create / update the database schema
dotnet ef database update

# (View the generated SQL without running it)
dotnet ef migrations script

# Run the API
dotnet run
```
Open **https://localhost:<port>/swagger** and try the endpoints.
Run tests: `dotnet test` from the solution root.

---

## 2. Run as a container (local)

The build context is the **solution root** (the Dockerfile copies the csproj from a subfolder).

```powershell
# from the solution root
docker build -f ExpenseTracker.Api/Dockerfile -t expense-tracker:local .

docker run -p 8080:8080 `
  -e ConnectionStrings__DefaultConnection="Server=<host>,1433;Database=ExpenseTrackerApi;User Id=sa;Password=<pwd>;TrustServerCertificate=True" `
  expense-tracker:local
```
Then: `http://localhost:8080/swagger` and `http://localhost:8080/health/live`.

> Note: `__` (double underscore) in the env var maps to `:` in ASP.NET Core config,
> so `ConnectionStrings__DefaultConnection` overrides `ConnectionStrings:DefaultConnection`.

---

## 3. Deploy to AWS (ECR + EKS + RDS) — runbook

Install first: **AWS CLI v2**, **eksctl**, **kubectl**, **helm**. Then `aws configure`.

Set these once in your shell (PowerShell example):
```powershell
$REGION="ap-south-1"; $ACCOUNT=(aws sts get-caller-identity --query Account --output text)
$ECR_REPO="expense-tracker"; $CLUSTER="expense-eks"
```

### 3.1 Create the ECR repository
```powershell
aws ecr create-repository --repository-name $ECR_REPO --region $REGION
```

### 3.2 Create the EKS cluster (takes ~15-20 min)
```powershell
eksctl create cluster --name $CLUSTER --region $REGION `
  --nodes 2 --node-type t3.medium --with-oidc --managed
```

### 3.3 Create RDS for SQL Server
```powershell
aws rds create-db-instance `
  --db-instance-identifier expense-sqlserver `
  --engine sqlserver-ex --engine-version 16.00 `
  --db-instance-class db.t3.small --allocated-storage 20 `
  --master-username admin --master-user-password "<STRONG_PASSWORD>" `
  --publicly-accessible false --region $REGION
```
- Wait until available; get the endpoint:
  `aws rds describe-db-instances --db-instance-identifier expense-sqlserver --query "DBInstances[0].Endpoint.Address" --output text`
- In the RDS security group, **allow inbound TCP 1433 from the EKS node security group** only.
- Create the `ExpenseTrackerApi` database on the instance (connect with SSMS/sqlcmd and `CREATE DATABASE ExpenseTrackerApi;`). The app's startup migration creates the tables.

### 3.4 Install the AWS Load Balancer Controller (for the ALB ingress)
Follow the official steps (IAM policy → `eksctl create iamserviceaccount` → `helm install`):
https://docs.aws.amazon.com/eks/latest/userguide/aws-load-balancer-controller.html

### 3.5 Create the namespace and the DB secret
```powershell
kubectl apply -f k8s/namespace.yaml

kubectl create secret generic db-conn -n expense-tracker `
  --from-literal=ConnectionStrings__DefaultConnection="Server=<RDS_ENDPOINT>,1433;Database=ExpenseTrackerApi;User Id=admin;Password=<PASSWORD>;TrustServerCertificate=True"
```

### 3.6 First deploy (manual, one time)
```powershell
# point the deployment at your image, then apply
# edit k8s/deployment.yaml image: <ACCOUNT>.dkr.ecr.<REGION>.amazonaws.com/expense-tracker:latest
kubectl apply -f k8s/service.yaml
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/ingress.yaml

kubectl get pods -n expense-tracker
kubectl get ingress -n expense-tracker   # open the ALB DNS name + /swagger
```

---

## 4. Automated CI/CD (GitHub Actions)

The pipeline (`.github/workflows/ci-cd.yml`) runs on push to `main`:
**build → test → docker build → push to ECR (tagged with commit SHA) → deploy to EKS.**
Auth uses **GitHub OIDC** (no AWS keys stored in GitHub).

### 4.1 Create the GitHub OIDC IAM role
1. Add GitHub as an OIDC identity provider in IAM (`token.actions.githubusercontent.com`).
2. Create an IAM role trusting your repo (`repo:<owner>/<repo>:ref:refs/heads/main`).
3. Attach permissions: ECR push/pull + `eks:DescribeCluster` (and map the role in `aws-auth`
   / EKS access entries so `kubectl` can deploy).

### 4.2 Add GitHub repo variables (Settings → Secrets and variables → Actions → Variables)
| Variable | Example |
|---|---|
| `AWS_REGION` | `ap-south-1` |
| `ECR_REPO` | `expense-tracker` |
| `EKS_CLUSTER` | `expense-eks` |
| `AWS_ROLE_ARN` | `arn:aws:iam::<account>:role/github-oidc-expense` |

Push to `main` → watch the run in the **Actions** tab. It ends with `kubectl rollout status`.

---

## 5. Cost & cleanup ⚠️
EKS control plane (~$0.10/hr) + 2× t3.medium nodes + RDS run up cost. When done practicing:
```powershell
eksctl delete cluster --name $CLUSTER --region $REGION
aws rds delete-db-instance --db-instance-identifier expense-sqlserver --skip-final-snapshot --region $REGION
aws ecr delete-repository --repository-name $ECR_REPO --force --region $REGION
```

---

## Notes
- **Auto-migration on startup** (`db.Database.Migrate()` in `Program.cs`) is convenient for
  learning. For production, prefer a dedicated Kubernetes **migration Job** so app pods don't
  race to migrate.
- Local dev uses SQL auth against your SQL Server; on AWS the same SQL-auth string points at RDS,
  injected via the `db-conn` secret (works on Linux containers — Windows auth would not).
