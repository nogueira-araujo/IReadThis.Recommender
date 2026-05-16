# IReadThis.Recommender

A sophisticated **AI-powered book recommendation engine** built with **.NET 10** and **TensorFlow.NET** that leverages deep learning neural networks to provide personalized book recommendations based on user profiles and reading preferences.

## 📚 Overview

IReadThis.Recommender is an **ASP.NET Core 10 Web API** that implements a **two-tower neural network architecture** to generate embeddings for both readers and books. The system uses TensorFlow.NET with GPU acceleration (CUDA) to train and serve machine learning models that match reader preferences with suitable book recommendations. The API runs on **HTTP/HTTPS** with singleton session management for efficient resource utilization.

### ✨ Key Features

- **Two-Tower Neural Network Architecture**: Separate deep learning networks for reader and book embeddings
- **GPU-Accelerated Computing**: Optimized for NVIDIA GPUs (tested with RTX 4070) using CUDA and cuDNN
- **Persistent Model Checkpoints**: Model state saved and restored from SQL Server for continuity
- **Graceful Hardware Shutdown**: Proper CUDA/GPU memory cleanup on application shutdown
- **Batch Processing**: Efficient book embedding generation for large catalogs
- **Real-time Recommendations**: 
  - Profile-based recommendations (registered user lookup)
  - Generic recommendations (cold-start scenario for new users)
- **REST API**: Lightweight endpoints via ASP.NET Core routing
- **Category-aware Embeddings**: Multi-dimensional category features in model training
- **Thread-safe Session Management**: Semaphore-based locking for concurrent operations
- **OpenAPI/Swagger Support**: Built-in API documentation endpoint

## 🏗️ Architecture

### Two-Tower Model

The system implements a proven recommendation architecture consisting of:

1. **Reader Tower**: Processes reader demographic features (birth year, gender) to generate reader embeddings
2. **Book Tower**: Processes book features (categories, title/description text) to generate book embeddings
3. **Matching Layer**: Computes similarity between reader and book embeddings using ratings for training

### TensorFlow Session Management

- **Singleton Session**: Single persistent TensorFlow session shared across all API requests for inference
- **Batch Sessions**: Temporary sessions created during training to avoid conflicts with the main session
- **Graceful Shutdown**: Dedicated hardware resource cleanup that releases CUDA/GPU memory when API stops
- **Thread Safety**: Semaphore-based synchronization for concurrent training operations

### Project Structure

```
IReadThis.Recommender/
├── Controllers/
│   ├── AdminController.cs          # Administrative operations & training triggers
│   ├── BookCatalogController.cs    # Book catalog retrieval endpoints
│   └── RecommendationController.cs # Recommendation endpoints
├── Models/
│   ├── Book.cs                     # Book entity
│   ├── IBook.cs                    # Book interface
│   ├── IBookCategoryRelationship.cs # Category relationship interface
│   ├── RecommendationTrainerData.cs# Training data model
│   └── TrainBatchData.cs           # Batch training model
├── Services/
│   ├── AI/
│   │   ├── RecommendationEngine.cs          # Core recommendation orchestration
│   │   ├── RecommendationService.cs         # Business logic for recommendations
│   │   ├── RecommendationTrainer.cs         # Model training orchestration
│   │   ├── BookTowerModel.cs                # Book tower neural network model
│   │   ├── ReaderTowerModel.cs              # Reader tower neural network model
│   │   ├── BookEmbeddingGenerator.cs        # Book embedding inference
│   │   ├── ReaderEmbeddingGenerator.cs      # Reader embedding inference
│   │   ├── BookTowerCoreBuilder.cs          # Book tower graph construction
│   │   ├── ReaderTowerCoreBuilder.cs        # Reader tower graph construction
│   │   ├── SessionManager.cs                # TensorFlow session lifecycle management
│   │   └── Tokenizer.cs                     # Text tokenization & feature encoding
│   └── DB/
│       ├── BookRepository.cs                # Book data access & persistence
│       └── ModelCheckpointRepository.cs     # Model state persistence (checkpoints)
├── SQLScripts/                     # Database initialization & data seeding
│   ├── 001.create database.sql    # Schema creation
│   ├── 002.fill data.sql          # Sample data loading
│   └── 003.create sidecar pattern tables.sql # Model checkpoint tables
├── Libs/
│   └── DynamicDtoCore/            # External library for dynamic DTO handling
├── Properties/
│   └── launchSettings.json        # Development server configuration (port 5179)
├── appsettings.json               # Production configuration
├── appsettings.Development.json   # Development configuration
├── Program.cs                     # Application startup & DI configuration
├── IReadThis.Recommender.csproj   # Project file (.NET 10)
├── IReadThis.Recommender.http     # REST client test file (VS Code REST Client)
└── README.md                      # This file
```

## 🚀 Getting Started

### Prerequisites

- **.NET 10 SDK** - Latest version recommended
- **SQL Server** 2019+ or **SQL Server Express** for data storage
- **Visual Studio 2026 Community** or higher (recommended) or VS Code
- **NVIDIA GPU** with CUDA Compute Capability 3.5+ (optional but recommended for performance)
  - **CUDA Toolkit 12.x** - For GPU acceleration
  - **cuDNN 9.x** - For deep learning operations
  - Tested and optimized for: **NVIDIA RTX 4070**

### System Requirements

- Minimum: 8 GB RAM, dual-core CPU
- Recommended: 16+ GB RAM, quad-core+ CPU, NVIDIA RTX GPU with 8+ GB VRAM

### Installation & Setup

1. **Clone the repository**
   ```powershell
   git clone https://github.com/nogueira-araujo/IReadThis.Recommender.git
   cd IReadThis.Recommender
   ```

2. **Install .NET 10 dependencies**
   ```powershell
   dotnet restore
   ```

3. **Set up the SQL Server database**

   **Option A: Using SQL Server Management Studio**
   ```bash
   # Execute SQL scripts in order:
   sqlcmd -S localhost -d master -i "SQLScripts\001.create database.sql"
   sqlcmd -S localhost -d IReadThis -i "SQLScripts\002.fill data.sql"
   sqlcmd -S localhost -d IReadThis -i "SQLScripts\003.create sidecar pattern tables.sql"
   ```

   **Option B: Using Visual Studio**
   - Open SQL Server Object Explorer
   - Run the scripts in the `SQLScripts/` folder in order

4. **Configure connection string**
   - Edit `appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=localhost;Initial Catalog=IReadThis;Integrated Security=SSPI;TrustServerCertificate=True;"
     }
   }
   ```
   - For development overrides, edit `appsettings.Development.json`

5. **Build the project**
   ```powershell
   dotnet build
   ```

6. **Run the application**
   ```powershell
   dotnet run
   ```

The API will be available at:
- **HTTP**: `http://localhost:5179`
- **OpenAPI Documentation**: `http://localhost:5179/openapi/v1.json`

### Enabling GPU Support

1. Install **CUDA Toolkit 12.x** from NVIDIA
2. Install **cuDNN 9.x** and add to system PATH
3. Verify installation:
   ```powershell
   nvcc --version
   ```
4. The application automatically detects GPU availability and uses it for acceleration

### Testing Endpoints

Use the included `.http` file for quick testing in Visual Studio or VS Code:
```
IReadThis.Recommender.http
```

Or use PowerShell:
```powershell
# Get book catalog
Invoke-WebRequest -Uri "http://localhost:5179/BookCatalog/" -Method Get

# Get recommendations by profile
Invoke-WebRequest -Uri "http://localhost:5179/Recommendation/profile/1" -Method Get

# Trigger model training
Invoke-WebRequest -Uri "http://localhost:5179/Admin/" -Method Post -ContentType "application/json"
```

## 📡 API Endpoints

### Base URL
```
http://localhost:5179
```

### Book Catalog Service

**Get all available books**
```http
GET /BookCatalog/
Accept: application/json
```

Response:
```json
[
  {
    "bookId": 1,
    "title": "The Great Gatsby",
    "author": "F. Scott Fitzgerald",
    "publisher": "Scribner",
    "releaseYear": 1925,
    "pageCount": 180,
    "embeddings": [...]
  }
]
```

---

### Recommendation Service

**Get personalized recommendations (Profile-Based)**
```http
GET /Recommendation/profile/{userId}
Accept: application/json
```

Parameters:
- `userId` (int, path parameter): The ID of the registered user profile

Example:
```http
GET /Recommendation/profile/15
```

Response:
```json
[
  {
    "bookId": 42,
    "title": "Recommended Book Title",
    "author": "Author Name",
    "matchScore": 0.95,
    "reason": "Based on your profile"
  }
]
```

---

**Get generic recommendations (Cold Start)**
```http
GET /Recommendation/generic?birthYear={year}&sex={gender}
Accept: application/json
```

Parameters:
- `birthYear` (int, query): User's birth year (e.g., 1990)
- `sex` (string, query): User's gender (M/F)

Example:
```http
GET /Recommendation/generic?birthYear=1990&sex=M
```

Response:
```json
[
  {
    "bookId": 8,
    "title": "Popular Book",
    "author": "Famous Author",
    "matchScore": 0.87,
    "reason": "Popular choice for your demographic"
  }
]
```

---

### Admin Service

**Trigger model training**
```http
POST /Admin/
Content-Type: application/json
```

Body:
```json
{
  "epochs": 10
}
```

This endpoint trains the neural networks on historical rating data and updates the persistent model checkpoints.

---

### API Documentation

**OpenAPI/Swagger Documentation** (Development environment):
```
GET /openapi/v1.json
```

Available at: `http://localhost:5179/openapi/v1.json` (Development only)

## 🧠 Machine Learning Model

### Neural Network Architecture

**Reader Tower**:
- Input Layer: Reader demographic features (birth year, gender)
- Embedding Layer: Dense feature representation
- Hidden Layers: Deep neural network for pattern learning
- Output: Reader embedding vector (typically 64-128 dimensions)

**Book Tower**:
- Input Layer: Book category IDs and text tokens
- Category Embedding Layer: Category feature extraction
- Text Embedding Layer: Title/description tokenization
- Hidden Layers: Deep neural network for feature synthesis
- Output: Book embedding vector (same dimensions as reader embedding)

**Training Process**:
1. Load historical user-book interaction ratings from database
2. Group categories by book ID for multi-hot encoding
3. Tokenize text features using the Tokenizer service
4. Create fixed-size batches (padding to consistent dimensions)
5. Train towers on rating prediction task
6. Validate model performance on held-out data
7. Persist best model state to SQL Server checkpoint tables

### Model Persistence

- **Checkpoint Storage**: Model weights and state saved to SQL Server
- **Incremental Loading**: Latest checkpoint automatically loaded on application startup
- **Query**: `ModelCheckpointRepository.LoadLatestCheckpointAsync(session)`
- **Persistence**: Automatic after training completes

### Embedding Generation

**Book Embeddings**:
- Generated during batch processing via `ProcessAndGenerateBookEmbeddingsAsync()`
- Creates temporary TensorFlow session to avoid conflicts
- Stores embeddings in SQL for later retrieval
- Used during recommendation inference

**Reader Embeddings**:
- Generated on-demand during recommendation requests
- Uses singleton session and `ReaderEmbeddingGenerator`
- Input: Birth year and gender
- Singleton instance: Registered in DI container for request reuse

### Training & Inference

- **Training Mode**: Uses temporary session, executes full graph operations
- **Inference Mode**: Uses singleton session, optimized for latency
- **Session Safety**: Semaphore ensures only one training operation at a time
- **Graceful Lifecycle**: Sessions properly disposed on API shutdown via `SessionManager`

## 🔧 Configuration & Setup

### appsettings.json Structure

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft.AspNetCore": "Warning"
	}
  },
  "AllowedHosts": "*",
  "Connection": "Default",
  "ConnectionStrings": {
	"Default": "Server=localhost;Initial Catalog=IReadThis;Integrated Security=SSPI;TrustServerCertificate=True;"
  },
  "DbProviders": {
	"Default": "Microsoft.Data.SqlClient, Microsoft.Data.SqlClient.SqlClientFactory"
  },
  "UseDbParameterName": true,
  "DbParameterPrefix": "@"
}
```

### Environment-Specific Configuration

- **Development** (`appsettings.Development.json`):
  - OpenAPI/Swagger endpoint enabled
  - Detailed logging output
  - Local SQL Server instance

- **Production** (`appsettings.json`):
  - OpenAPI disabled for security
  - Warning-level logging only
  - Production SQL Server connection

### Launch Settings

The application is configured to run on **port 5179** by default (see `Properties/launchSettings.json`):
- **HTTP**: `http://localhost:5179`
- **Development**: Includes hot reload support

### TensorFlow Configuration

Located in `Program.cs`:

```csharp
private static bool gpu = false; // Set to true to force GPU usage
```

**Automatic GPU Detection**:
- Application automatically detects available NVIDIA GPUs
- Allocates optimal memory based on system resources
- Falls back to CPU if GPU unavailable

**GPU Memory Management**:
- **On Startup**: Initializes TensorFlow with GPU context
- **On Shutdown**: Gracefully releases all CUDA/GPU memory
- **During Runtime**: Managed by TensorFlow session lifecycle

### Database Configuration

**Connection String Components**:
- `Server`: SQL Server instance (localhost for local development)
- `Initial Catalog`: Database name (IReadThis)
- `Integrated Security`: Windows authentication (SSPI)
- `TrustServerCertificate`: Self-signed certificate trust (development only)

**Sidecar Pattern for Model Storage**:
- Checkpoints stored in SQL Server tables (not filesystem)
- Enables consistent state across application restarts
- Facilitates distributed deployment scenarios

## 🧪 Development & Testing

### Build

```powershell
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Build with configuration
dotnet build -c Release
```

### Run Locally

```powershell
# Run with development settings
dotnet run --environment Development

# Run with hot reload
dotnet watch run

# Run in release mode
dotnet run -c Release
```

### Testing Endpoints

**Using VS Code REST Client** (`.http` file):
```
IReadThis.Recommender.http
```

**Using cURL**:
```bash
# Get book catalog
curl http://localhost:5179/BookCatalog/

# Get recommendations
curl http://localhost:5179/Recommendation/profile/1

# Generic recommendations
curl "http://localhost:5179/Recommendation/generic?birthYear=1990&sex=M"

# Trigger training
curl -X POST http://localhost:5179/Admin/ \
  -H "Content-Type: application/json" \
  -d '{"epochs": 10}'
```

**Using PowerShell**:
```powershell
$bookCatalog = Invoke-RestMethod -Uri "http://localhost:5179/BookCatalog/"
$recommendations = Invoke-RestMethod -Uri "http://localhost:5179/Recommendation/profile/1"
```

### Debug Mode

1. Open Visual Studio
2. Set breakpoints in desired code
3. Press **F5** or click Debug → Start Debugging
4. Hit endpoints via REST client or browser

### Application Lifecycle

1. **Startup** (`Program.cs`):
   - Initialize WebApplication builder
   - Create TensorFlow session
   - Load latest model checkpoint from database
   - Register all services in DI container
   - Configure middleware and routes

2. **Runtime**:
   - Handle incoming HTTP requests
   - Generate embeddings on-demand
   - Serve recommendations with inference
   - Execute optional training operations

3. **Shutdown**:
   - Trigger graceful shutdown via `ApplicationStopping` event
   - Dispose TensorFlow session
   - Release GPU/CUDA memory
   - Close database connections

## 📦 Dependencies & NuGet Packages

### Core Framework
- **Microsoft.AspNetCore.OpenApi 10.0.7** - OpenAPI/Swagger support
- **Microsoft.Extensions.Configuration 10.0.7** - Configuration management
- **Microsoft.Extensions.Configuration.Json 10.0.7** - JSON config file support

### AI/ML Stack
- **TensorFlow.NET 0.150.0** - TensorFlow bindings for .NET
- **SciSharp.TensorFlow.Redist-Windows-GPU 2.10.3** - GPU-optimized TensorFlow distributions
- **NumSharp 0.30.0** - NumPy-like numerical computing

### Database & Data Access
- **Microsoft.Data.SqlClient 7.0.1** - SQL Server connectivity

### Custom Libraries
- **DynamicDtoCore** - Custom library for dynamic DTO generation (included in `Libs/` directory)

### Framework
- **.NET 10.0 SDK** - Latest .NET runtime and compilation target

## 💾 Database Architecture

### Schema Overview

**Core Tables**:
- **Books**: Book catalog with metadata
  - `BookID` (PK), `Title`, `Author`, `Publisher`, `ReleaseYear`, `PageCount`

- **BookCategories**: Multi-to-many relationship
  - `BookID`, `CategoryID`

- **Profiles**: User profiles with demographic data
  - `ProfileID` (PK), `BirthYear`, `Sex`, `Name`

- **Ratings**: User-book interactions
  - `ProfileID`, `BookID`, `Rating`

**Checkpoint/Sidecar Tables** (Pattern for AI model persistence):
- **ModelCheckpoints**: Persisted model state
  - Stores TensorFlow session graph and variable weights
  - Timestamps and versioning for rollback capability
  - Query results in `ModelCheckpointRepository`

### Data Initialization

Run SQL scripts in order:
1. **001.create database.sql** - Schema and table definitions
2. **002.fill data.sql** - Sample book data and user profiles
3. **003.create sidecar pattern tables.sql** - Checkpoint storage tables

### Access Patterns

- **Repository Pattern**: Abstracted data access via `BookRepository` and `ModelCheckpointRepository`
- **DynamicDtoCore**: Dynamic object creation from query results
- **Connection Pooling**: Automatic via `Microsoft.Data.SqlClient`

## 🔐 Security Considerations

### Application Security

- **HTTPS Redirect**: Enabled in production middleware
- **Authorization**: Configured via ASP.NET Core authorization pipeline
- **Input Validation**: All query parameters validated before processing
- **SQL Injection Prevention**: Parameterized queries via `DynamicDtoCore` and SQL parameters
- **Sensitive Configuration**: Database credentials managed via connection strings (user secrets in development)

### API Security

- **CORS Policy**: Configured in middleware as needed
- **Rate Limiting**: Consider implementing for production (not currently enabled)
- **API Keys**: Can be added to admin endpoints for training triggers
- **Request Validation**: Record types enforce type safety on POST/PUT bodies

### Data Security

- **Database Encryption**: Enable Transparent Data Encryption (TDE) on production SQL Server
- **Connection Security**: Use Windows authentication or Azure AD for SQL connections
- **Environment Variables**: Sensitive data via environment variables, not hardcoded
- **Audit Logging**: Enable SQL Server audit for compliance requirements

### GPU/Hardware Security

- **Memory Access**: TensorFlow handles GPU memory isolation
- **Process Isolation**: Run API in dedicated application pool
- **Resource Limits**: Configure SQL Server memory and resource governance

## 🐛 Troubleshooting

### Common Issues & Solutions

#### 1. TensorFlow Session Error
**Problem**: "Session is already closed" or "Operation must be in default graph"

**Solutions**:
- Ensure only one session is active (singleton management)
- Check that semaphore properly limits concurrent training
- Verify checkpoint loading completes before serving requests
- Check `SessionManager.Dispose()` not called prematurely

**Code Reference**: `Program.cs` → `HardwareShutdown()` method

#### 2. Database Connection Failure
**Problem**: "Connection to SQL Server failed" or "timeout"

**Solutions**:
- Verify SQL Server is running: `services.msc` → check "SQL Server (SQLEXPRESS)"
- Test connection string in SSMS
- Ensure database initialization scripts executed in order
- Check firewall allows SQL Server port (default: 1433)
- Verify Windows authentication enabled or use SQL auth credentials

**Command**: 
```powershell
sqlcmd -S localhost -E -Q "SELECT @@VERSION"
```

#### 3. GPU/CUDA Issues
**Problem**: "No CUDA devices found" or "OutOfMemory on GPU"

**Solutions**:
- Verify NVIDIA GPU present: `nvidia-smi`
- Check CUDA Toolkit installed: `nvcc --version`
- Verify cuDNN properly installed and in PATH
- Monitor GPU memory: `nvidia-smi -l 1`
- Reduce batch size in training if OOM occurs
- Set `gpu = false` to fall back to CPU

#### 4. Model Checkpoint Not Found
**Problem**: "Checkpoint loading failed" or "No checkpoints in database"

**Solutions**:
- Run initial training via `/Admin/` endpoint to create checkpoint
- Verify checkpoint table created: `SELECT COUNT(*) FROM ModelCheckpoints;`
- Check latest checkpoint timestamp in database
- Inspect `ModelCheckpointRepository.cs` for query issues
- Review SQL Script `003.create sidecar pattern tables.sql`

#### 5. Port Already in Use
**Problem**: "Address already in use - 5179"

**Solutions**:
```powershell
# Find process using port
Get-NetTCPConnection -LocalPort 5179 | Select ProcessId

# Kill process
Stop-Process -Id <ProcessId> -Force

# Or use different port in launchSettings.json
```

#### 6. Embedding Generation Errors
**Problem**: "Embeddings are null" or "Shape mismatch in tensor"

**Solutions**:
- Verify category data properly formatted in database
- Check tokenizer produces consistent output sizes
- Ensure padding matches tower model expectations (typically 50)
- Validate text data not empty or corrupted
- Check `BookTowerCoreBuilder` and `ReaderTowerCoreBuilder` output shapes

#### 7. OutOfMemory Exception
**Problem**: ".NET heap exhausted" or "native memory exhausted"

**Solutions**:
- Reduce batch processing size
- Implement pagination for large result sets
- Monitor memory usage: `Get-Process IReadThis.Recommender`
- Check for memory leaks in TensorFlow session
- Configure max memory in `Program.cs` before session creation

### Enable Debug Logging

**In `appsettings.Development.json`**:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

**View logs in**:
- Visual Studio Debug Output window (Ctrl+Alt+O)
- Browser Console (F12)
- Application event log (Event Viewer)

## 📈 Performance Optimization

### Current Optimizations

- **Singleton TensorFlow Session**: Reuses session across requests, avoiding initialization overhead
- **Batch Processing**: Book embeddings generated in single batch for efficiency
- **Connection Pooling**: SQL connections automatically pooled by `Microsoft.Data.SqlClient`
- **Embedding Caching**: Generated embeddings cached in database for fast retrieval
- **Asynchronous Operations**: All I/O operations use async/await patterns

### Optimization Strategies

**For Large-Scale Deployments**:

1. **Distributed Inference**: Scale read replicas for recommendation serving
   - Use load balancer to distribute requests
   - Each instance maintains independent session

2. **Model Caching**: Cache popular recommendation results
   - Redis or in-memory cache for frequent queries
   - TTL-based invalidation

3. **GPU Optimization**:
   - Use `TensorRT` for model quantization and optimization
   - Batch API requests for higher throughput
   - Monitor GPU utilization via `nvidia-smi`

4. **Database Optimization**:
   - Create indexes on `Books`, `Profiles`, `Ratings` tables
   - Archive old ratings for cold storage
   - Use materialized views for frequent aggregations

5. **Asynchronous Processing**:
   - Queue training jobs for non-blocking execution
   - Use background services for embedding generation
   - Consider Azure Functions for serverless scaling

**Benchmarking**:
```powershell
# Measure recommendation latency
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$result = Invoke-RestMethod -Uri "http://localhost:5179/Recommendation/profile/1"
$sw.Stop()
Write-Host "Response time: $($sw.ElapsedMilliseconds)ms"
```

## 🤝 Contributing

We welcome contributions to improve the recommendation engine! 

### Development Workflow

1. **Fork the repository**
   ```bash
   gh repo fork nogueira-araujo/IReadThis.Recommender
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/YourFeatureName
   ```

3. **Make your changes**
   - Follow existing code style and patterns
   - Add comments for complex logic
   - Test thoroughly with various inputs

4. **Build and test locally**
   ```powershell
   dotnet build
   dotnet run --environment Development
   ```

5. **Commit with descriptive messages**
   ```bash
   git commit -m "feat: Add new recommendation algorithm

   - Improved matching accuracy
   - Reduced inference latency by 15%
   - Added unit tests for new logic"
   ```

6. **Push to your fork**
   ```bash
   git push origin feature/YourFeatureName
   ```

7. **Open a Pull Request**
   - Describe the changes and motivation
   - Reference any related issues (#123)
   - Request review from maintainers

### Code Guidelines

- **Language**: C# 11+, .NET 10
- **Style**: Follow Microsoft C# Coding Conventions
- **Naming**: PascalCase for public members, camelCase for private
- **Async**: Prefer async/await for I/O operations
- **Nullability**: Utilize nullable reference types (#nullable enable)
- **Comments**: Document public APIs and complex algorithms

### Testing

- Add unit tests for new features
- Test with multiple GPU configurations
- Verify database operations with SQL Server
- Test edge cases (null inputs, empty datasets)

### Documentation

- Update README.md for user-facing changes
- Add XML documentation comments to public APIs
- Include performance implications in PR description

### Areas for Contribution

- **Model Improvements**: Enhanced neural network architectures
- **Performance**: Optimization of TensorFlow operations
- **Features**: Cold-start improvements, diversity in recommendations
- **DevOps**: Docker containerization, Kubernetes deployment
- **Documentation**: API guides, deployment tutorials, troubleshooting
- **Testing**: Integration tests, performance benchmarks

## 📄 License

This project is open source and available under the [MIT License](LICENSE).

## 📧 Contact & Support

For issues, questions, or suggestions:

1. **GitHub Issues**: [Report a bug](https://github.com/nogueira-araujo/IReadThis.Recommender/issues/new)
2. **Discussions**: [Start a discussion](https://github.com/nogueira-araujo/IReadThis.Recommender/discussions)
3. **Email**: Contact the maintainers directly

## 🎓 References & Resources

### Machine Learning & Recommendation Systems
- [Two-Tower Recommendation Models - Google Research](https://research.google/pubs/pub50583/)
- [Neural Collaborative Filtering](https://arxiv.org/abs/1708.05024)
- [Deep Learning for Recommendation Systems](https://arxiv.org/abs/1708.02530)

### TensorFlow & Deep Learning
- [TensorFlow.NET GitHub Repository](https://github.com/SciSharp/TensorFlow.NET)
- [TensorFlow Official Documentation](https://www.tensorflow.org/api_docs)
- [TensorFlow.NET Getting Started](https://github.com/SciSharp/TensorFlow.NET#getting-started)

### ASP.NET Core & .NET
- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core)
- [.NET 10 Release Notes](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-10)
- [Dependency Injection in .NET](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)

### Database & Performance
- [SQL Server Performance Tuning](https://learn.microsoft.com/sql/relational-databases/performance/performance-center-for-sql-server-database-engine)
- [NumSharp Documentation](https://github.com/SciSharp/NumSharp)
- [Connection Pooling Best Practices](https://learn.microsoft.com/sql/connect/ado-net/connection-pooling)

### GPU & CUDA
- [NVIDIA CUDA Toolkit Documentation](https://docs.nvidia.com/cuda/index.html)
- [cuDNN Installation Guide](https://docs.nvidia.com/deeplearning/cudnn/installation/index.html)
- [GPU Memory Management in TensorFlow](https://www.tensorflow.org/guide/gpu)

---

## 📊 Project Status

- **Current Version**: 1.0.0
- **Status**: Active Development
- **Last Updated**: 2024
- **Built With**: 
  - .NET 10 SDK
  - TensorFlow.NET
  - ASP.NET Core 10
  - SQL Server
  - NVIDIA CUDA/cuDNN

## 🎯 Roadmap

Planned features and improvements:

- [ ] Docker containerization for easy deployment
- [ ] Kubernetes manifests for cloud-native deployment
- [ ] A/B testing framework for recommendation improvements
- [ ] Advanced cold-start strategies
- [ ] Diversity in recommendations
- [ ] Real-time model retraining
- [ ] GraphQL API alternative
- [ ] Mobile app integration examples
- [ ] Advanced analytics dashboard
- [ ] Multi-model ensemble support

## 🏆 Acknowledgments

- Built with [TensorFlow.NET](https://github.com/SciSharp/TensorFlow.NET) by SciSharp Community
- Inspired by Google's Two-Tower recommendation architecture
- Community contributions and feedback
