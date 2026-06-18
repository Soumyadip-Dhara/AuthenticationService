using AngleSharp.Text;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using UserManagement.BAL.Interfaces;
using UserManagement.BAL.Interfaces.Master;
using UserManagement.DAL.Entities;
using UserManagement.DAL.Interfaces;
using UserManagement.DAL.Interfaces.Master;
using UserManagement.Models.DTO;

namespace UserManagement.BAL.Services.Master
{
    public class JWTService(
        IMapper mapper,
        IConfiguration configuration,
        IApplicationRepository applicationRepository,
        IUserHasApplicationRepository userHasApplicationRepository,
        IUserApplicationHasUserRoleRepository userHasUserRoleRepository,
        IUserRoleHasUserLevelRepository userRoleHasUserLevelRepository,
        IUserRoleHasUserPermissionRepository userRoleHasUserPermissionRepository,
        IUserLevelHasUserScopeRepository userLevelHasUserScopeRepository,
        IScopeRepository scopeRepository,
        IClaimService claimService,
        IUserMasterRepository userMasterRepository,
        ILoginLogRepository loginLogRepository,
        IUserRoleScopeAppContextRepository userRoleScopeAppContextRepository
    ) : IJWTService
    {
        private readonly IMapper _mapper = mapper;
        private readonly IConfiguration _configuration = configuration;
        private readonly IApplicationRepository _applicationRepository = applicationRepository;
        private readonly IUserHasApplicationRepository _userHasApplicationRepository = userHasApplicationRepository;
        private readonly IUserApplicationHasUserRoleRepository _userApplicationHasUserRoleRepository = userHasUserRoleRepository;
        private readonly IUserRoleHasUserLevelRepository _userRoleHasUserLevelRepository = userRoleHasUserLevelRepository;
        private readonly IUserRoleHasUserPermissionRepository _userRoleHasUserPermissionRepository = userRoleHasUserPermissionRepository;
        private readonly IUserLevelHasUserScopeRepository _userLevelHasUserScopeRepository = userLevelHasUserScopeRepository;
        private readonly IScopeRepository _scopeRepository = scopeRepository;
        private readonly IClaimService _claimService = claimService;
        private readonly IUserMasterRepository _userMasterRepository = userMasterRepository;
        private readonly ILoginLogRepository _loginLogRepository = loginLogRepository;
        private readonly IUserRoleScopeAppContextRepository _userRoleScopeAppContextRepository = userRoleScopeAppContextRepository;

        private static object _lock = new object();

        private string _accessTokenIssuedAt = null!;
        private string _refreshTokenIssuedAt = null!;
        private DateTime _accessTokenNotBefore;
        private DateTime _refreshTokenNotBefore;
        private DateTime _accessTokenExpirationTime;
        private DateTime _refreshTokenExpirationTime;

        private DateTime _currentIstDateTime;
        private static readonly Dictionary<string, AuthTokenForModules> _issuedTokens = [];

        private static void RemoveExpiredTokens(int maxRefreshTokenValidityInSecs = 3600)
        {
            JwtSecurityTokenHandler tokenHandler = new();

            _issuedTokens.Where(
                c => {
                    return long.Parse(tokenHandler.ReadJwtToken(c.Value.AccessToken)
                        .Claims.FirstOrDefault(
                            c => c.Type == JwtRegisteredClaimNames.Exp
                        )?.Value ?? "0") <= (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - maxRefreshTokenValidityInSecs);
                })
                .Select(c => c.Key)
                .ToList().ForEach(
                    item =>
                    {
                        _issuedTokens.Remove(item);
                        Console.WriteLine($"Removing Token: {item}");
                    }
                );
        }

        private void SetTokenTimes(
            double accessTokenValidityInMinutes = 5,
            double refreshTokenValidityInMinutes = 10
        )
        {
            _currentIstDateTime = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
            );
            _accessTokenIssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            _accessTokenNotBefore = DateTime.UtcNow;
#if DEBUG
            _accessTokenExpirationTime = DateTime.UtcNow.AddDays(accessTokenValidityInMinutes);
#else
            _accessTokenExpirationTime = DateTime.UtcNow.AddMinutes(accessTokenValidityInMinutes);
#endif

            _refreshTokenIssuedAt = _accessTokenIssuedAt;
            _refreshTokenNotBefore = _accessTokenExpirationTime;
#if DEBUG
            _refreshTokenExpirationTime = _accessTokenExpirationTime.AddDays(refreshTokenValidityInMinutes);
#else
            _refreshTokenExpirationTime = _accessTokenExpirationTime.AddMinutes(refreshTokenValidityInMinutes);
#endif
        }

        public async Task<AuthTokenForModules> IssueJwtToken(
            int appId,
            Claim[] jwtClaims,
            double accessTokenValidityInMinutes = 5,
            double refreshTokenValidityInMinutes = 1,
            long userId = 0,
            Guid? sessionId =  null
        )
        {
            
            string accessToken = "", refreshToken = "", appName = "", appUrl = "";
            long previousTokenId = _claimService.GetPrevTokenId();
            long currentTokenId = _claimService.GetTokenId();
            string tokenCacheKey = $"{currentTokenId}";
            try
            {
                SetTokenTimes(
                    accessTokenValidityInMinutes,
                    refreshTokenValidityInMinutes
                );
                if (sessionId == null)
                {
                    sessionId = Guid.Parse(_claimService.GetSessionId());
                }

                string[] claimFilter = [
                    "aud",
                    "aid",
                    "pti",
                    "sid",
                    JwtRegisteredClaimNames.Typ,
                    JwtRegisteredClaimNames.Jti,
                    JwtRegisteredClaimNames.Iat
                ];

                IEnumerable<Claim> jwtDataPayload = [.. jwtClaims.Where(
                    c => !claimFilter.Any(ctype => ctype == c.Type)
                )];

                string jwtAccessTokenSecretKey = _configuration["Auth:SecretKey"]!;
                string jwtRefreshTokenSecretKey = jwtAccessTokenSecretKey;

                if (appId != 0)
                {
                    var appData = await _applicationRepository.GetSingleSelectedColumnByConditionAsync(
                        e => e.Id == appId,
                        e => new
                        {
                            Key = e.Key,
                            Name = e.Title,
                            Url = e.Url,
                        }
                    );
                    appName = appData.Name;
                    appUrl = appData.Url;
                    jwtAccessTokenSecretKey = appData.Key;
                }

                SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtAccessTokenSecretKey));
                SigningCredentials accessTokenSigningCredentials = new(
                    key,
                    SecurityAlgorithms.HmacSha512
                );

                key = new(Encoding.UTF8.GetBytes(jwtRefreshTokenSecretKey));
                SigningCredentials refreshTokenSigningCredentials = new(
                    key,
                    SecurityAlgorithms.HmacSha512
                );

                if (userId == 0)
                {
                    userId = _claimService.GetUserId();
                }

                if (_configuration["Session:singleSession"].ToBoolean())
                {
                    _loginLogRepository.DeleteRange(
                        await _loginLogRepository.GetAllByConditionAsync(
                            e => e.UserId == userId
                            && e.ApplicationId != appId
                        )
                    );
                }

                lock (_lock)
                {
                    RemoveExpiredTokens();
                    Console.WriteLine($"TokenID: {currentTokenId} is being used to issue token.");
                    if (currentTokenId != 0 && _issuedTokens.ContainsKey(tokenCacheKey))
                    {
                        Console.WriteLine($"TokenID: {currentTokenId} found in Cache.");
                        if (_issuedTokens.TryGetValue(tokenCacheKey, out AuthTokenForModules? tokenFromCache))
                        {
                            return tokenFromCache;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"TokenID: {currentTokenId} not found with Cache with key:{tokenCacheKey}.");
                    }

                    LoginLog? loginLog = new()
                    {
                        ApplicationId = appId,
                        UserId = userId,
                        LoginTime = _currentIstDateTime,
                        SessionId = sessionId
                    };

                    _loginLogRepository.Add(loginLog);
                    _loginLogRepository.SaveChangesManaged();

                    List<Claim> accessTokenClaims = [.. jwtDataPayload,
                        new Claim("typ", "acc"),
                        new Claim("pti", _claimService.GetTokenId().ToString()),
                        new Claim("aid", appId.ToString() ?? "0"),
                        new Claim ("sid" ,sessionId.ToString()),
                        new Claim(JwtRegisteredClaimNames.Jti, loginLog.Id.ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, _accessTokenIssuedAt, ClaimValueTypes.Integer64),
                    ];
                    JwtSecurityToken accessTokenData = new(
                        _configuration["Jwt:Issuer"],
                        _configuration["Jwt:Audience"],
                        accessTokenClaims,
                        _accessTokenNotBefore,
                        _accessTokenExpirationTime,
                        accessTokenSigningCredentials
                    );

                    accessToken = new JwtSecurityTokenHandler().WriteToken(accessTokenData);

                    List<Claim> refreshTokenClaims = [.. jwtDataPayload,
                        new Claim("typ", "ref"),
                        new Claim("pti", _claimService.GetTokenId().ToString()),
                        new Claim("aid", appId.ToString() ?? "0"),
                        new Claim ("sid" ,sessionId.ToString()),
                        new Claim(JwtRegisteredClaimNames.Jti, loginLog.Id.ToString()),
                        new Claim(JwtRegisteredClaimNames.Iat, _refreshTokenIssuedAt, ClaimValueTypes.Integer64),
                    ];

                    JwtSecurityToken refreshTokenData = new(
                        _configuration["Jwt:Issuer"],
                        _configuration["Jwt:Audience"],
                        refreshTokenClaims,
                        _refreshTokenNotBefore,
                        _refreshTokenExpirationTime,
                        refreshTokenSigningCredentials
                    );

                    refreshToken = new JwtSecurityTokenHandler().WriteToken(refreshTokenData);
#if DEBUG
                    Console.WriteLine($"Deactivated: {previousTokenId}");
                    Console.WriteLine($"User: {userId} Acc: {_claimService.GetTokenId()}:{loginLog.Id} => {accessToken}");
                    Console.WriteLine($"User: {userId} Ref: {_claimService.GetTokenId()}:{loginLog.Id} => {refreshToken}");
#endif
                    if (currentTokenId != 0)
                    {                        
                        _issuedTokens.Add(
                            tokenCacheKey,
                            new()
                            {
                                AccessToken = accessToken,
                                RefreshToken = refreshToken,
                                RefreshTokenValidityInMinutes = refreshTokenValidityInMinutes,
                                GuidToken = refreshTokenValidityInMinutes.ToString(),
                                Url = appUrl,
                                ApplicationName = appName
                            }

                        );
                        Console.WriteLine($"TokenID: {loginLog.Id} cached with key: {tokenCacheKey}. Cached Tokens: {_issuedTokens.Count}");
                    }
#if DEBUG
                    _issuedTokens
                        .Select(
                            i => {
                                string key = i.Key;
                                string refreshToken = i.Value?.RefreshToken ?? "...null-ref";
                                string accessToken = i.Value?.AccessToken ?? "...null-acc";
                                double refreshTokenValidityInMinutes = i.Value?.RefreshTokenValidityInMinutes ?? -1;
                                accessToken = accessToken[(accessToken.Length - 8)..accessToken.Length];
                                refreshToken = refreshToken[(refreshToken.Length - 8)..refreshToken.Length];
                                return $"{key} => acc: {accessToken} ref: {refreshToken} time: {refreshTokenValidityInMinutes}";
                            }
                        )
                        .ToList()
                        .ForEach(Console.WriteLine);
#endif
                    Console.WriteLine($"TokenID: {loginLog.Id} issued against TokenID: {currentTokenId} User: {userId}");
                }
                _loginLogRepository.DeleteRange(
                    await _loginLogRepository.GetAllByConditionAsync(
                        e => e.Id == previousTokenId
                    )
                );
                _loginLogRepository.SaveChangesManaged();
            }
            catch (Exception ex)
            {
                return new()
                {
#if DEBUG
                    AccessToken = ex.ToString()
#endif
                };
            }

            return new()
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Url = appUrl,
                RefreshTokenValidityInMinutes = refreshTokenValidityInMinutes,
                ApplicationName = appName
            };
        }


        public async Task<AuthTokenForModules> JWTTokenCreationForUMAndMM(DataCollectionJWTDTO dataCollectionDTO)    // done
        {
            int appId = dataCollectionDTO.AppId;
            int userId = _claimService.GetUserId();
            var userHasApplication = await _userHasApplicationRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.AppId == appId && e.UserId == userId,
                e => new
                {
                    AppName = e.App.Title,
                    AppId = e.AppId,
                    PrimaryKeyId = e.Id,
                    Username = e.User.Name,
                    SignerID = e.User.SignerId
                });
            var userApplicationHasRole = await _userApplicationHasUserRoleRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.UserHasAppId == userHasApplication.PrimaryKeyId,
                e => new
                {
                    RoleName = e.Role.Title,
                    RoleId = e.RoleId,
                    PrimaryKeyId = e.Id
                });
            var userRoleHasUserLevel = await _userRoleHasUserLevelRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.ApplicationHasRoleId == userApplicationHasRole.PrimaryKeyId,
                e => new
                {
                    LevelName = e.RoleHasLevel.Level != null ? e.RoleHasLevel.Level.LevelName : string.Empty,
                    LevelId = e.RoleHasLevel.AppLevelId,
                    PrimayKey = e.Id
                });
            var userRoleHasUserPermission = (List<string>)await _userRoleHasUserPermissionRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationHasRoleId == userApplicationHasRole.PrimaryKeyId,
                e => e.RoleHasPermission.Name);
            var userLevelHasUserScope = await _userLevelHasUserScopeRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.UserRoleHasLevelId == userRoleHasUserLevel.PrimayKey && e.LevelId == userRoleHasUserLevel.LevelId,
                e => new
                {
                    ScopeId = e.UserLevelHasScopeId,
                });
            //var scope = await _scopeRepository.GetSingleSelectedColumnByConditionAsync(
            //    e => userLevelHasUserScope.ScopeId == e.Id,
            //    e => new
            //    {
            //        ScopeName = e.Name,
            //        ScopeId = e.Id
            //    });

            var scope = await _scopeRepository.GetScopesByScopeIds(new List<long> { userLevelHasUserScope.ScopeId });



            var jwtDataPayload = new ApplicationDTO
            {
                Id = userHasApplication.AppId,
                Name = userHasApplication.AppName,
                Role = new RoleDTO
                {
                    Id = userApplicationHasRole.RoleId,
                    Name = userApplicationHasRole.RoleName,
                    Level = new LevelDTO
                    {
                        Id = userRoleHasUserLevel.LevelId,
                        Name = userRoleHasUserLevel.LevelName,
                        Scope = scope[0].ScopeValue,
                        ScopeId = scope[0].ScopeId,
                    },
                    Permissions = userRoleHasUserPermission,
                }
            };


            var appData = await _applicationRepository.GetSingleSelectedColumnByConditionAsync(e => e.Id == appId, e => new
            {
                Key = e.Key,
                Name = e.Title,
                Url = e.Url,
            });
            double accessTokenValidityInMinutes = Double.Parse(_configuration["TimeInMinutes:UMMM"] ?? "10");
            double refreshTokenValidityInMinutes = Double.Parse(_configuration["TimeInMinutes:UMMMRefresh"] ?? "10");

            AuthTokenForModules tokens = await IssueJwtToken(
                appId,
                [
                    new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                    new Claim("nameid", userId.ToString()),
                    new Claim("name" , userHasApplication.Username),
                    new Claim("signerId" , userHasApplication.SignerID)
                ],
                accessTokenValidityInMinutes,
                refreshTokenValidityInMinutes
            );

            string g = Guid.NewGuid().ToString();
            _userMasterRepository.SetJWTAccessToken(g, (tokens.AccessToken ?? "", tokens.RefreshToken ?? ""));
            return new AuthTokenForModules
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                Url = appData.Url,
                GuidToken = g,
                RefreshTokenValidityInMinutes = refreshTokenValidityInMinutes,
                ApplicationName = appData.Name

            };
        }

        public async Task<AuthTokenForModules> JWTTokenCreationForUMAndMMForMultiple(GetJWTPayload applicationData) // done
        {


            var appId = applicationData.Application[0].Id;
            int userId = _claimService.GetUserId();
            var userHasApplication = await _userHasApplicationRepository.GetSingleSelectedColumnByConditionAsync(
               e => e.AppId == appId && e.UserId == userId,
               e => new
               {
                   AppName = e.App.Title,
                   AppId = e.AppId,
                   PrimaryKeyId = e.Id,
                   Username = e.User.Name,
                   SignerID = e.User.SignerId
               });
            var appData = await _applicationRepository.GetSingleSelectedColumnByConditionAsync(e => e.Id == applicationData.Application[0].Id, e => new
            {
                Key = e.Key,
                Name = e.Title,
                Url = e.Url,
            });
            var jwtDataPayload = new object();
            jwtDataPayload = applicationData.Application[0];

            double accessTokenValidityInMinutes = Double.Parse(_configuration["TimeInMinutes:UMMM"] ?? "10");
            double refreshTokenValidityInMinutes = Double.Parse(_configuration["TimeInMinutes:UMMMRefresh"] ?? "10");


            AuthTokenForModules tokens = await IssueJwtToken(
                appId,
                [
                    new Claim("application", JsonConvert.SerializeObject(jwtDataPayload)),
                    new Claim("nameid", applicationData.UserId.ToString()),
                    new Claim("name" , _claimService.GetUserName()),
                    new Claim("signerId" ,  userHasApplication.SignerID ?? "")
                ],
                accessTokenValidityInMinutes,
                refreshTokenValidityInMinutes
            );

            string g = Guid.NewGuid().ToString();
            _userMasterRepository.SetJWTAccessToken(g, (tokens.AccessToken ?? "", tokens.RefreshToken ?? ""));
            return new AuthTokenForModules
            {
                AccessToken = tokens.AccessToken,
                RefreshToken = tokens.RefreshToken,
                GuidToken = g,
                RefreshTokenValidityInMinutes = refreshTokenValidityInMinutes,
                Url = appData.Url,
                ApplicationName = appData.Name
            };
        }

        public async Task<AuthTokenForModules> JWTTokenCreationForOtherForMultiple(GetJWTPayload applicationData)   // done
        {
            var appId = applicationData.Application[0].Id;
            int userId = _claimService.GetUserId();
            var userHasApplication = await _userHasApplicationRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.AppId == appId && e.UserId == userId,
                e => new
                {
                    UserName = e.User.Name,
                    Email = e.User.Email,
                    PhoneNumber = e.User.MobileNumber,
                    UserCreatedBy = e.User.CreatedBy,
                    PrimaryKeyId = e.Id,
                    Designation = e.User.Designation,
                    UserId = e.User.UserName,
                    SignerID = e.User.SignerId
                });
            var userApplicationHasRole = await _userApplicationHasUserRoleRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.UserHasAppId == userHasApplication.PrimaryKeyId,
                e => new
                {
                    RoleName = e.Role.Title,
                    RoleId = e.RoleId,
                    PrimaryKeyId = e.Id
                });
            var userRoleHasUserLevel = await _userRoleHasUserLevelRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.ApplicationHasRoleId == userApplicationHasRole.PrimaryKeyId,
                e => new
                {
                    LevelName = e.RoleHasLevel.Level != null ? e.RoleHasLevel.Level.LevelName : string.Empty,
                    LevelId = e.RoleHasLevel.AppLevelId,
                    PrimayKey = e.Id
                });
            var userRoleHasUserPermission = (List<string>)await _userRoleHasUserPermissionRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationHasRoleId == userApplicationHasRole.PrimaryKeyId,
                e => e.RoleHasPermission.Name);
            var userLevelHasUserScope = await _userLevelHasUserScopeRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.UserRoleHasLevelId == userRoleHasUserLevel.PrimayKey && e.LevelId == userRoleHasUserLevel.LevelId,
                e => new
                {
                    ScopeId = e.UserLevelHasScopeId,
                    id = e.Id
                });
            var scope = await _scopeRepository.GetScopesByScopeIds(new List<long> { userLevelHasUserScope.ScopeId });
            var userLevelScopeContext = await _userRoleScopeAppContextRepository.GetSingleSelectedColumnByConditionAsync(

                e => e.ApplicationId == appId && e.UserLevelHasScopeId == userLevelHasUserScope.id,
                e => e.OptionalJson
            );

            var appData = await _applicationRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.Id == applicationData.Application[0].Id,
                e => new
                {
                    Key = e.Key,
                    Name = e.Title,
                    Url = e.Url,
                }
            );

            var jwtDataPayload = new ApplicationDTO();
            jwtDataPayload = applicationData.Application[0];
            var userDetail = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
                    e => e.Id == _claimService.GetUserId(),
                    e => new { e.Email, e.CreatedBy, e.Designation, e.MobileNumber, e.UserName }
                );
            var creatorUser = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.Id == userDetail.CreatedBy,
                e => new { e.UserName }
            );

            var claims = new List<Claim>();

            var parentScopeValue = await _scopeRepository.GetParentScopeValuesAsync(
                jwtDataPayload.Role.Level.Scope.ToString(),
                jwtDataPayload.Role.Level.Id
            );

            if (!string.IsNullOrWhiteSpace(userLevelScopeContext))
            {
                var parsed = JsonConvert.DeserializeObject<JObject>(userLevelScopeContext);

                foreach (var prop in parsed.Properties())
                {
                    var key = prop.Name;
                    var value = prop.Value?.ToString();

                    // Special handling for "optional" (since it's a JSON string)
                    if (key == "optional" && !string.IsNullOrEmpty(value))
                    {
                        var optionalObj = JsonConvert.DeserializeObject(value);
                        value = JsonConvert.SerializeObject(optionalObj, Formatting.None);
                    }

                    // Only add if value exists
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        claims.Add(new Claim(key, value));
                    }
                }
            }


            // =========================
            // ADD OTHER CLAIMS
            // =========================
            claims.AddRange(new[]
            {
       new Claim("role", jwtDataPayload.Role.Name),
                    new Claim("roleId", jwtDataPayload.Role.Id.ToString()),
                    new Claim("permissions", JsonConvert.SerializeObject(jwtDataPayload.Role.Permissions)),
                    new Claim("level", jwtDataPayload.Role.Level.Name),
                    new Claim("levelId", jwtDataPayload.Role.Level.Id.ToString()),
                    new Claim("scope", jwtDataPayload.Role.Level.Scope),
                    new Claim("scopeId", jwtDataPayload.Role.Level.ScopeId.ToString()),
                    new Claim("nameid", applicationData.UserId.ToString()),
                    new Claim("email", !string.IsNullOrEmpty(userDetail.Email) ? userDetail.Email.ToString() : ""),
                    new Claim("phoneNumber", userDetail.MobileNumber),
                    new Claim("name" , _claimService.GetUserName()),
                    new Claim("created_by" , creatorUser?.UserName ?? ""),
                    new Claim("designation" , userDetail.Designation.ToString()),
                    new Claim("userid" , userDetail.UserName.ToString()),
                    new Claim("finyear" , _configuration["CurrentFinancialYear"]??""),
                    //new Claim("finyear" , FinancialYearHelper.GetCurrentFinancialYear().ToString()),
                    new Claim("parent_scope" , parentScopeValue?.FirstOrDefault() ?? ""),
                    new Claim("signerId" ,  userHasApplication.SignerID ?? "")

    });

            // =========================
            // ISSUE TOKEN
            // =========================
            AuthTokenForModules jwtToken = await IssueJwtToken(
                appId,
                claims.ToArray(),
                accessTokenValidityInMinutes: Double.Parse(_configuration["TimeInMinutes:OtherApp"] ?? "10"),
                refreshTokenValidityInMinutes: Double.Parse(_configuration["TimeInMinutes:OtherAppRefresh"] ?? "10")
            );

            return new AuthTokenForModules
            {
                Url = appData.Url,
                AccessToken = jwtToken.AccessToken,
                RefreshToken = jwtToken.RefreshToken,
                ApplicationName = appData.Name,
            };
        }

        public async Task<AuthTokenForModules> JWTTokenCreationForOther(DataCollectionJWTDTO dataCollectionDTO)   // done
        {
            int appId = dataCollectionDTO.AppId;
            int userId = _claimService.GetUserId();
            var userHasApplication = await _userHasApplicationRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.AppId == appId && e.UserId == userId,
                e => new
                {
                    UserName = e.User.Name,
                    Email = e.User.Email,
                    PhoneNumber = e.User.MobileNumber,
                    UserCreatedBy = e.User.CreatedBy,
                    PrimaryKeyId = e.Id,
                    Designation = e.User.Designation,
                    UserId = e.User.UserName,
                    SignerID = e.User.SignerId
                });
            var userApplicationHasRole = await _userApplicationHasUserRoleRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.UserHasAppId == userHasApplication.PrimaryKeyId,
                e => new
                {
                    RoleName = e.Role.Title,
                    RoleId = e.RoleId,
                    PrimaryKeyId = e.Id
                });
            var userRoleHasUserLevel = await _userRoleHasUserLevelRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.ApplicationHasRoleId == userApplicationHasRole.PrimaryKeyId,
                e => new
                {
                    LevelName = e.RoleHasLevel.Level != null ? e.RoleHasLevel.Level.LevelName : string.Empty,
                    LevelId = e.RoleHasLevel.AppLevelId,
                    PrimayKey = e.Id
                });
            var userRoleHasUserPermission = (List<string>)await _userRoleHasUserPermissionRepository.GetSelectedColumnByConditionAsync(
                e => e.ApplicationHasRoleId == userApplicationHasRole.PrimaryKeyId,
                e => e.RoleHasPermission.Name);
            var userLevelHasUserScope = await _userLevelHasUserScopeRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.UserRoleHasLevelId == userRoleHasUserLevel.PrimayKey && e.LevelId == userRoleHasUserLevel.LevelId,
                e => new
                {
                    ScopeId = e.UserLevelHasScopeId,
                    id = e.Id
                });
            var scope = await _scopeRepository.GetScopesByScopeIds(new List<long> { userLevelHasUserScope.ScopeId });
            var userLevelScopeContext = await _userRoleScopeAppContextRepository.GetSingleSelectedColumnByConditionAsync(

                e => e.ApplicationId == appId && e.UserLevelHasScopeId == userLevelHasUserScope.id,
                e => e.OptionalJson
            );

            var appData = await _applicationRepository.GetSingleSelectedColumnByConditionAsync(e => e.Id == appId, e => new
            {
                Key = e.Key,
                Name = e.Title,
                Url = e.Url,
            });

            var userDetail = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
                    e => e.Id == _claimService.GetUserId(),
                    e => new { e.Email, e.CreatedBy, e.Designation, e.MobileNumber, e.UserName }
                );

            var creatorUser = await _userMasterRepository.GetSingleSelectedColumnByConditionAsync(
                e => e.Id == userDetail.CreatedBy,
                e => new { e.UserName }
            );

            var claims = new List<Claim>();
            var scopeValue = scope.FirstOrDefault()?.ScopeValue;

            var parentScopeValue = await _scopeRepository.GetParentScopeValuesAsync(scopeValue, userRoleHasUserLevel.LevelId);

            if (!string.IsNullOrWhiteSpace(userLevelScopeContext))
            {
                var parsed = JsonConvert.DeserializeObject<JObject>(userLevelScopeContext);

                foreach (var prop in parsed.Properties())
                {
                    var key = prop.Name;
                    var value = prop.Value?.ToString();

                    // Special handling for "optional" (since it's a JSON string)
                    if (key == "optional" && !string.IsNullOrEmpty(value))
                    {
                        var optionalObj = JsonConvert.DeserializeObject(value);
                        value = JsonConvert.SerializeObject(optionalObj, Formatting.None);
                    }

                    // Only add if value exists
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        claims.Add(new Claim(key, value));
                    }
                }
            }

            // ADD OTHER CLAIMS
            claims.AddRange(new[]
            {
                new Claim("scope", scopeValue),
                new Claim("role", userApplicationHasRole.RoleName ?? ""),
                new Claim("roleId", userApplicationHasRole.RoleId.ToString()),
                new Claim("permissions", JsonConvert.SerializeObject(userRoleHasUserPermission ?? new List<string>())),
                new Claim("level", userRoleHasUserLevel.LevelName ?? ""),
                new Claim("levelId", userRoleHasUserLevel.LevelId.ToString()),
                new Claim("scopeId", userLevelHasUserScope.ScopeId.ToString()),
                new Claim("nameid", userId.ToString()),
                new Claim("email", userHasApplication.Email ?? ""),
                new Claim("phoneNumber", userHasApplication.PhoneNumber ?? ""),
                new Claim("name", userHasApplication.UserName ?? ""),
                new Claim("created_by" , creatorUser?.UserName ?? ""),
                new Claim("designation", userHasApplication.Designation?.ToString() ?? ""),
                new Claim("userid" ,userHasApplication.UserId.ToString()),
                new Claim("finyear" , _configuration["CurrentFinancialYear"]??""),
                //new Claim("finyear" , FinancialYearHelper.GetCurrentFinancialYear().ToString()),
                new Claim("parent_scope" , parentScopeValue.FirstOrDefault()?.ToString() ?? ""),
                new Claim("signerId" ,  userHasApplication.SignerID ?? "")
            });

            // =========================
            // ISSUE TOKEN
            // =========================
            AuthTokenForModules jwtToken = await IssueJwtToken(
                appId,
                claims.ToArray(),
                accessTokenValidityInMinutes: Double.Parse(_configuration["TimeInMinutes:OtherApp"] ?? "10"),
                refreshTokenValidityInMinutes: Double.Parse(_configuration["TimeInMinutes:OtherAppRefresh"] ?? "10")
            );

            return new AuthTokenForModules
            {
                Url = appData.Url,
                AccessToken = jwtToken.AccessToken,
                RefreshToken = jwtToken.RefreshToken,
                ApplicationName = appData.Name,
            };
        }
    }
}
