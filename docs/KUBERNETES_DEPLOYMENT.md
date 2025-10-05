# Kubernetes Deployment Guide for Mordecai MUD

This guide explains how to deploy Mordecai MUD to Kubernetes, including configuration for RabbitMQ and PostgreSQL database.

## Recent Updates (October 2025)

**RabbitMQ Migration:** The application has been migrated from Aspire-managed local RabbitMQ to direct connection with a Kubernetes-hosted RabbitMQ instance. Key changes:

- ✅ Removed `Aspire.RabbitMQ.Client` package dependency
- ✅ Cloud-native configuration with environment variables taking priority
- ✅ Password stored in User Secrets (local) and Kubernetes Secrets (production)
- ✅ Direct connection to `default-rabbitmq-amqp` RabbitMQ service
- ✅ Fail-fast validation ensures required configuration is present at startup

See [RABBITMQ_KUBERNETES_MIGRATION.md](../RABBITMQ_KUBERNETES_MIGRATION.md) for detailed migration information.

## Overview

Mordecai MUD is now configured to support Kubernetes deployment with the following features:

- **Direct RabbitMQ Connection**: Cloud-native configuration with explicit connection parameters
- **PostgreSQL Database**: Cloud-native database with proper secret management
- **Cloud-Native Configuration**: Environment variables and Kubernetes Secrets for all sensitive data
- **Optional Aspire Integration**: Aspire can still be used for local development orchestration

## Configuration Architecture

### RabbitMQ Configuration

The application uses a **cloud-native configuration approach** with direct RabbitMQ connection parameters. Aspire RabbitMQ integration has been removed in favor of explicit configuration.

#### Cloud-Native Configuration (Production)
```json
{
  "RabbitMQ": {
    "Host": "default-rabbitmq-amqp",
    "Port": 5672,
    "Username": "mordecai",
    "VirtualHost": "/"
  }
}
```

**Important:** The `Password` field is intentionally omitted from `appsettings.json` and must be provided via User Secrets (local development) or Kubernetes Secrets (production).

**Environment Variable Override Priority (Cloud-Native Best Practice):**
1. `RABBITMQ_HOST` (env var) → `RabbitMQ:Host` (config)
2. `RABBITMQ_PORT` (env var) → `RabbitMQ:Port` (config)
3. `RABBITMQ_USERNAME` (env var) → `RabbitMQ:Username` (config)
4. `RABBITMQ_PASSWORD` (env var) → `RabbitMQ:Password` (User Secrets/K8s Secrets) - **REQUIRED**
5. `RABBITMQ_VIRTUALHOST` (env var) → `RabbitMQ:VirtualHost` (config)

**Configuration Validation:** The application will throw an exception at startup if required configuration (Host, Username, Password) is missing, ensuring fail-fast behavior.

### Database Configuration

PostgreSQL connection is configured via individual parameters for cloud-native secret management:

**Configuration Priority:**
1. Environment variables (highest priority): `Database__Host`, `Database__Port`, `Database__Name`, `Database__User`, `Database__Password`
2. User Secrets (development only)
3. `appsettings.json` (non-sensitive values only)

**Required Configuration:**
```json
{
  "Database": {
    "Host": "postgres-service",
    "Port": "5432",
    "Name": "mordecai",
    "User": "mordecaimud"
  }
}
```

**Password:** Must be provided via Kubernetes Secret (never in configuration files)

**Environment Variable Format:**
```bash
Database__Host=postgres-service
Database__Port=5432
Database__Name=mordecai
Database__User=mordecaimud
Database__Password=<from-secret>
```

## Kubernetes Deployment

### Prerequisites

- Kubernetes cluster (1.25+)
- kubectl configured
- PostgreSQL database (StatefulSet or managed service)
- RabbitMQ deployment or external RabbitMQ service

### Step 1: Create Namespace

```yaml
# namespace.yaml
apiVersion: v1
kind: Namespace
metadata:
  name: mordecai-mud
```

```bash
kubectl apply -f namespace.yaml
```

### Step 2: Create Secrets

Store sensitive configuration in Kubernetes secrets:

```yaml
# secrets.yaml
apiVersion: v1
kind: Secret
metadata:
  name: mordecai-secrets
  namespace: mordecai-mud
type: Opaque
stringData:
  # Database credentials
  db-password: your-secure-db-password
  
  # RabbitMQ credentials
  # Note: Username is in appsettings.json, only password stored in secret
  rabbitmq-password: your-secure-rabbitmq-password
```

```bash
kubectl apply -f secrets.yaml
```

**Using External Secret Management:**

For production, consider using External Secrets Operator with Azure Key Vault, AWS Secrets Manager, or HashiCorp Vault:

```yaml
# external-secret.yaml
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: mordecai-secrets
  namespace: mordecai-mud
spec:
  secretStoreRef:
    name: azure-keyvault-store  # or aws-secrets-manager, vault, etc.
    kind: SecretStore
  target:
    name: mordecai-secrets
    creationPolicy: Owner
  data:
  - secretKey: db-password
    remoteRef:
      key: mordecai-db-password
  - secretKey: rabbitmq-password
    remoteRef:
      key: mordecai-rabbitmq-password
```

### Step 3: Deploy PostgreSQL

#### Option A: PostgreSQL StatefulSet (Development/Testing)

```yaml
# postgres-statefulset.yaml
apiVersion: v1
kind: Service
metadata:
  name: postgres-service
  namespace: mordecai-mud
spec:
  selector:
    app: postgres
  ports:
  - port: 5432
    targetPort: 5432
  clusterIP: None
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: postgres
  namespace: mordecai-mud
spec:
  serviceName: postgres-service
  replicas: 1
  selector:
    matchLabels:
      app: postgres
  template:
    metadata:
      labels:
        app: postgres
    spec:
      containers:
      - name: postgres
        image: postgres:16
        ports:
        - containerPort: 5432
        env:
        - name: POSTGRES_DB
          value: mordecai
        - name: POSTGRES_USER
          value: mordecaimud
        - name: POSTGRES_PASSWORD
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: db-password
        - name: PGDATA
          value: /var/lib/postgresql/data/pgdata
        volumeMounts:
        - name: postgres-storage
          mountPath: /var/lib/postgresql/data
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "1000m"
  volumeClaimTemplates:
  - metadata:
      name: postgres-storage
    spec:
      accessModes: [ "ReadWriteOnce" ]
      resources:
        requests:
          storage: 20Gi
```

```bash
kubectl apply -f postgres-statefulset.yaml
```

#### Option B: Managed Database Service (Production)

For production, use a managed PostgreSQL service:

- **Azure**: Azure Database for PostgreSQL Flexible Server
- **AWS**: Amazon RDS for PostgreSQL
- **GCP**: Cloud SQL for PostgreSQL
- **DigitalOcean**: Managed Databases

Update the secrets with your managed database connection details:

```yaml
stringData:
  db-host: your-managed-db.postgres.database.azure.com
  db-password: your-managed-db-password
```

### Step 4: Deploy RabbitMQ (if not using external service)

```yaml
# rabbitmq-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: rabbitmq
  namespace: mordecai-mud
spec:
  replicas: 1
  selector:
    matchLabels:
      app: rabbitmq
  template:
    metadata:
      labels:
        app: rabbitmq
    spec:
      containers:
      - name: rabbitmq
        image: rabbitmq:3.13-management
        ports:
        - containerPort: 5672
          name: amqp
        - containerPort: 15672
          name: management
        env:
        - name: RABBITMQ_DEFAULT_USER
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: rabbitmq-username
        - name: RABBITMQ_DEFAULT_PASS
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: rabbitmq-password
---
apiVersion: v1
kind: Service
metadata:
  name: rabbitmq-service
  namespace: mordecai-mud
spec:
  selector:
    app: rabbitmq
  ports:
  - name: amqp
    port: 5672
    targetPort: 5672
  - name: management
    port: 15672
    targetPort: 15672
```

```bash
kubectl apply -f rabbitmq-deployment.yaml
```

### Step 5: Deploy Mordecai Web Application

```yaml
# mordecai-web-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mordecai-web
  namespace: mordecai-mud
spec:
  replicas: 2  # Scale based on player load - PostgreSQL supports multiple replicas
  selector:
    matchLabels:
      app: mordecai-web
  template:
    metadata:
      labels:
        app: mordecai-web
    spec:
      containers:
      - name: mordecai-web
        image: your-registry/mordecai-web:latest
        ports:
        - containerPort: 8080
          name: http
        env:
        # Database Configuration
        - name: Database__Host
          value: "postgres-service"  # Or managed database hostname
        - name: Database__Port
          value: "5432"
        - name: Database__Name
          value: "mordecai"
        - name: Database__User
          value: "mordecaimud"
        - name: Database__Password
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: db-password
        
        # RabbitMQ Configuration
        - name: RABBITMQ_HOST
          value: "default-rabbitmq-amqp"
        - name: RABBITMQ_PORT
          value: "5672"
        - name: RABBITMQ_USERNAME
          value: "mordecai"
        - name: RABBITMQ_PASSWORD
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: rabbitmq-password
        - name: RABBITMQ_VIRTUALHOST
          value: "/"
        
        # ASP.NET Core Configuration
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:8080"
        
        resources:
          requests:
            memory: "512Mi"
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "2000m"
        
        livenessProbe:
          httpGet:
            path: /alive
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 5
---
apiVersion: v1
kind: Service
metadata:
  name: mordecai-web-service
  namespace: mordecai-mud
spec:
  selector:
    app: mordecai-web
  ports:
  - name: http
    port: 80
    targetPort: 8080
  type: LoadBalancer  # Or ClusterIP with Ingress
```

```bash
kubectl apply -f mordecai-web-deployment.yaml
```

### Step 6: Configure Ingress (Optional)

```yaml
# ingress.yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: mordecai-ingress
  namespace: mordecai-mud
  annotations:
    cert-manager.io/cluster-issuer: letsencrypt-prod
    nginx.ingress.kubernetes.io/websocket-services: mordecai-web-service
spec:
  ingressClassName: nginx
  tls:
  - hosts:
    - mordecai.yourdomain.com
    secretName: mordecai-tls
  rules:
  - host: mordecai.yourdomain.com
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: mordecai-web-service
            port:
              number: 80
```

```bash
kubectl apply -f ingress.yaml
```

## Using Aspire for Manifest Generation

**Note:** Aspire RabbitMQ integration has been removed. The application now connects directly to RabbitMQ using explicit configuration.

.NET Aspire can still be used for local development orchestration and manifest generation:

### Generate Manifests

```bash
# From the Mordecai.AppHost project (if still using Aspire AppHost)
dotnet run --project Mordecai.AppHost --publisher manifest --output-path ./manifests
```

This generates deployment manifests that you can customize further.

### Local Development

For local development, you have two options:

1. **Direct Connection**: Configure RabbitMQ connection in `appsettings.json` and User Secrets (recommended)
2. **Aspire AppHost**: Use the AppHost project for local service orchestration (optional)

## Environment Variables Reference

### Required Environment Variables

| Variable | Description | Example |
|----------|-------------|---------|
| `Database__Host` | PostgreSQL hostname or service name | `postgres-service` |
| `Database__Port` | PostgreSQL port | `5432` |
| `Database__Name` | PostgreSQL database name | `mordecai` |
| `Database__User` | PostgreSQL username | `mordecaimud` |
| `Database__Password` | PostgreSQL password (from secret) | `<secret>` |
| `RABBITMQ_HOST` | RabbitMQ hostname or service name | `default-rabbitmq-amqp` |
| `RABBITMQ_PORT` | RabbitMQ AMQP port | `5672` |
| `RABBITMQ_USERNAME` | RabbitMQ username | `mordecai` |
| `RABBITMQ_PASSWORD` | RabbitMQ password (from secret) | `<secret>` |

### Optional Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `RABBITMQ_VIRTUALHOST` | RabbitMQ virtual host | `/` |
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Production` |
| `ASPNETCORE_URLS` | URLs to listen on | `http://+:8080` |

## Database Considerations

### PostgreSQL Configuration

**✅ Production-Ready Features:**
- **Multiple Replicas**: PostgreSQL supports multiple application instances reading/writing simultaneously
- **ACID Compliance**: Full transaction support for game state consistency
- **Horizontal Scaling**: Scale application pods independently of database
- **Managed Services**: Use cloud provider managed databases for automatic backups, updates, and high availability

### Connection Pooling

Entity Framework Core automatically manages connection pooling. For high-traffic scenarios, consider:

```yaml
# Add to deployment environment variables
- name: Database__MaxPoolSize
  value: "100"
- name: Database__MinPoolSize
  value: "10"
```

### Database Migrations

Run migrations as an init container or Kubernetes Job before deployment:

```yaml
# db-migration-job.yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: mordecai-db-migration
  namespace: mordecai-mud
spec:
  template:
    spec:
      containers:
      - name: migration
        image: your-registry/mordecai-web:latest
        command: ["dotnet", "ef", "database", "update"]
        env:
        - name: Database__Host
          value: "postgres-service"
        - name: Database__Port
          value: "5432"
        - name: Database__Name
          value: "mordecai"
        - name: Database__User
          value: "mordecaimud"
        - name: Database__Password
          valueFrom:
            secretKeyRef:
              name: mordecai-secrets
              key: db-password
      restartPolicy: OnFailure
```

## Monitoring and Observability

### OpenTelemetry Integration

The application uses OpenTelemetry for distributed tracing:

```yaml
# Add to deployment environment variables
- name: OTEL_EXPORTER_OTLP_ENDPOINT
  value: "http://otel-collector:4317"
- name: OTEL_SERVICE_NAME
  value: "mordecai-web"
```

### Health Checks

- **Liveness**: `/alive` - Basic application health
- **Readiness**: `/health` - Includes dependency checks (RabbitMQ, Database)

## Scaling Considerations

### Horizontal Pod Autoscaling

```yaml
# hpa.yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: mordecai-web-hpa
  namespace: mordecai-mud
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: mordecai-web
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

**Note:** PostgreSQL supports true horizontal scaling of application pods. Scale based on player load and resource utilization.

### RabbitMQ Scaling

Consider RabbitMQ clustering for high availability:

```yaml
# Use RabbitMQ cluster operator or StatefulSet
# See: https://www.rabbitmq.com/kubernetes/operator/operator-overview.html
```

## Security Best Practices

### 1. Use Kubernetes Secrets
- Store all credentials in secrets
- Never commit secrets to version control
- Use external secret management (e.g., Azure Key Vault, AWS Secrets Manager)

### 2. Network Policies
```yaml
# network-policy.yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: mordecai-network-policy
  namespace: mordecai-mud
spec:
  podSelector:
    matchLabels:
      app: mordecai-web
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - namespaceSelector:
        matchLabels:
          name: ingress-nginx
    ports:
    - protocol: TCP
      port: 8080
  egress:
  - to:
    - podSelector:
        matchLabels:
          app: rabbitmq
    ports:
    - protocol: TCP
      port: 5672
```

### 3. Pod Security Standards
```yaml
# Add to deployment spec
securityContext:
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 1000
  seccompProfile:
    type: RuntimeDefault
containers:
- name: mordecai-web
  securityContext:
    allowPrivilegeEscalation: false
    capabilities:
      drop:
      - ALL
    readOnlyRootFilesystem: true
```

## Troubleshooting

### Database Connection Issues

```bash
# Check database connection environment variables
kubectl exec -it -n mordecai-mud <pod-name> -- env | grep Database__

# Test PostgreSQL connectivity from pod
kubectl exec -it -n mordecai-mud <pod-name> -- bash
> apt-get update && apt-get install -y postgresql-client
> psql -h postgres-service -U mordecaimud -d mordecai

# Check PostgreSQL service
kubectl get svc -n mordecai-mud postgres-service

# Check PostgreSQL logs
kubectl logs -n mordecai-mud postgres-0

# Verify secret exists
kubectl get secret -n mordecai-mud mordecai-secrets
kubectl describe secret -n mordecai-mud mordecai-secrets
```

### RabbitMQ Connection Issues

```bash
# Check RabbitMQ service DNS
kubectl exec -it -n mordecai-mud <pod-name> -- nslookup rabbitmq-service

# Test RabbitMQ connection
kubectl exec -it -n mordecai-mud <pod-name> -- curl rabbitmq-service:15672

# Check RabbitMQ logs
kubectl logs -n mordecai-mud -l app=rabbitmq

# Verify environment variables
kubectl exec -it -n mordecai-mud <pod-name> -- env | grep RABBITMQ
```

### Application Logs

```bash
# View application logs
kubectl logs -n mordecai-mud -l app=mordecai-web --tail=100 -f

# Check for startup errors
kubectl logs -n mordecai-mud <pod-name> --previous
```

## Backup and Recovery

### PostgreSQL Backup

#### Manual Backup

```bash
# Create backup using pg_dump
kubectl exec -n mordecai-mud postgres-0 -- \
  pg_dump -U mordecaimud -d mordecai -F c -f /tmp/mordecai-backup-$(date +%Y%m%d).dump

# Copy backup locally
kubectl cp mordecai-mud/postgres-0:/tmp/mordecai-backup-$(date +%Y%m%d).dump \
  ./mordecai-backup-$(date +%Y%m%d).dump
```

#### Automated Backup CronJob

```yaml
# backup-cronjob.yaml
apiVersion: batch/v1
kind: CronJob
metadata:
  name: mordecai-db-backup
  namespace: mordecai-mud
spec:
  schedule: "0 2 * * *"  # Daily at 2 AM
  jobTemplate:
    spec:
      template:
        spec:
          containers:
          - name: backup
            image: postgres:16
            command:
            - sh
            - -c
            - |
              BACKUP_FILE="/backups/mordecai-$(date +%Y%m%d-%H%M%S).dump"
              pg_dump -h postgres-service -U mordecaimud -d mordecai -F c -f "$BACKUP_FILE"
              echo "Backup created: $BACKUP_FILE"
              # Cleanup old backups (keep last 7 days)
              find /backups -name "mordecai-*.dump" -mtime +7 -delete
            env:
            - name: PGPASSWORD
              valueFrom:
                secretKeyRef:
                  name: mordecai-secrets
                  key: db-password
            volumeMounts:
            - name: backup-storage
              mountPath: /backups
          restartPolicy: OnFailure
          volumes:
          - name: backup-storage
            persistentVolumeClaim:
              claimName: mordecai-backup-pvc
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: mordecai-backup-pvc
  namespace: mordecai-mud
spec:
  accessModes:
    - ReadWriteMany
  resources:
    requests:
      storage: 50Gi
```

#### Restore from Backup

```bash
# Restore database from backup
kubectl cp ./mordecai-backup-20251005.dump mordecai-mud/postgres-0:/tmp/restore.dump
kubectl exec -n mordecai-mud postgres-0 -- \
  pg_restore -U mordecaimud -d mordecai -c /tmp/restore.dump
```

### Managed Database Backups

If using a managed PostgreSQL service, use the cloud provider's backup features:

- **Azure Database for PostgreSQL**: Automated backups with point-in-time restore
- **AWS RDS**: Automated backups and manual snapshots
- **GCP Cloud SQL**: Automated backups and on-demand backups
- **DigitalOcean**: Automated daily backups

## Development vs Production

### Local Development

#### Option 1: Direct Configuration (Recommended)

```bash
# Set User Secrets for RabbitMQ password
cd Mordecai.Web
dotnet user-secrets set "RabbitMQ:Password" "<your-secure-password>"

# Run the application
dotnet run
```

The application connects to:
- RabbitMQ: `default-rabbitmq-amqp:5672` (configured in appsettings.json)
- PostgreSQL: Configured via environment variables or User Secrets

#### Option 2: Aspire AppHost (Optional)

```bash
# Start Aspire AppHost (if you want local orchestration)
dotnet run --project Mordecai.AppHost
```

Note: Aspire no longer manages RabbitMQ connection. You still need to configure RabbitMQ connection details in appsettings.json and User Secrets.

### Production Deployment

Use explicit environment variables for all configuration:
- No automatic service discovery
- Explicit connection parameters via environment variables
- Passwords from Kubernetes Secrets
- Persistent storage for database
- External RabbitMQ service or cluster

## Additional Security Considerations

### Secret Rotation

Rotate database and RabbitMQ passwords regularly:

```bash
# Update secret (example with new RabbitMQ password)
kubectl create secret generic mordecai-secrets \
  --from-literal=db-password=NEW_DB_PASSWORD \
  --from-literal=rabbitmq-password=NEW_RABBITMQ_PASSWORD \
  --namespace mordecai-mud \
  --dry-run=client -o yaml | kubectl apply -f -

# Restart pods to pick up new secrets
kubectl rollout restart deployment/mordecai-web -n mordecai-mud
```

**Important:** Update User Secrets for local development when you rotate passwords:
```bash
dotnet user-secrets set "RabbitMQ:Password" "NEW_RABBITMQ_PASSWORD" --project Mordecai.Web
dotnet user-secrets set "Database:Password" "NEW_DB_PASSWORD" --project Mordecai.Web
```

### Using Azure Key Vault (Azure AKS)

```yaml
# Install Azure Key Vault Provider for Secrets Store CSI Driver
# Then create SecretProviderClass
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: azure-keyvault-provider
  namespace: mordecai-mud
spec:
  provider: azure
  parameters:
    usePodIdentity: "false"
    useVMManagedIdentity: "true"
    userAssignedIdentityID: "<CLIENT_ID>"
    keyvaultName: "mordecai-keyvault"
    objects: |
      array:
        - |
          objectName: "db-password"
          objectType: "secret"
        - |
          objectName: "rabbitmq-password"
          objectType: "secret"
    tenantId: "<TENANT_ID>"
  secretObjects:
  - secretName: mordecai-secrets
    type: Opaque
    data:
    - objectName: "db-password"
      key: "db-password"
    - objectName: "rabbitmq-password"
      key: "rabbitmq-password"
```

### Using AWS Secrets Manager (AWS EKS)

```yaml
# Install AWS Secrets Manager CSI Driver
# Then create SecretProviderClass
apiVersion: secrets-store.csi.x-k8s.io/v1
kind: SecretProviderClass
metadata:
  name: aws-secrets-provider
  namespace: mordecai-mud
spec:
  provider: aws
  parameters:
    objects: |
      - objectName: "mordecai/db-password"
        objectType: "secretsmanager"
      - objectName: "mordecai/rabbitmq-password"
        objectType: "secretsmanager"
  secretObjects:
  - secretName: mordecai-secrets
    type: Opaque
    data:
    - objectName: "mordecai/db-password"
      key: "db-password"
    - objectName: "mordecai/rabbitmq-password"
      key: "rabbitmq-password"
```

### Service Mesh Integration

For advanced traffic management and observability, consider integrating with:
- Istio
- Linkerd
- Consul

### GitOps Deployment

Implement continuous deployment with:
- ArgoCD
- Flux
- GitHub Actions + Helm

## Support and Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Kubernetes Documentation](https://kubernetes.io/docs/)
- [RabbitMQ on Kubernetes](https://www.rabbitmq.com/kubernetes/operator/operator-overview.html)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)

## Configuration Validation

Before deploying, validate your configuration:

```bash
# Check all secrets are created
kubectl get secrets -n mordecai-mud

# Verify secret contents (keys only, not values)
kubectl describe secret mordecai-secrets -n mordecai-mud

# Test database connectivity
kubectl run -it --rm postgres-test \
  --image=postgres:16 \
  --restart=Never \
  --namespace=mordecai-mud \
  --env="PGPASSWORD=$(kubectl get secret mordecai-secrets -n mordecai-mud -o jsonpath='{.data.db-password}' | base64 -d)" \
  -- psql -h postgres-service -U mordecaimud -d mordecai -c "SELECT version();"

# Dry-run deployment to check for errors
kubectl apply -f mordecai-web-deployment.yaml --dry-run=client
```

## Performance Tuning

### PostgreSQL Optimization

```yaml
# Add to PostgreSQL StatefulSet or configure in managed service
env:
- name: POSTGRES_SHARED_BUFFERS
  value: "256MB"
- name: POSTGRES_EFFECTIVE_CACHE_SIZE
  value: "1GB"
- name: POSTGRES_MAX_CONNECTIONS
  value: "200"
- name: POSTGRES_WORK_MEM
  value: "4MB"
```

### Application Settings

```yaml
# Add to application deployment
env:
- name: Database__CommandTimeout
  value: "30"
- name: Database__MaxPoolSize
  value: "100"
- name: ASPNETCORE_Kestrel__Limits__MaxConcurrentConnections
  value: "1000"
```

---

**Last Updated:** October 5, 2025  
**Version:** 3.0 - Cloud-Native RabbitMQ Configuration  
**Changes:** Migrated from Aspire-managed RabbitMQ to direct Kubernetes service connection  
**Maintainer:** Mordecai MUD Development Team

**Related Documentation:**
- [RabbitMQ Kubernetes Migration](../RABBITMQ_KUBERNETES_MIGRATION.md)
- [RabbitMQ Quick Start Guide](../RABBITMQ_QUICK_START.md)
- [RabbitMQ Kubernetes Configuration](../RABBITMQ_KUBERNETES_CONFIG.md)
