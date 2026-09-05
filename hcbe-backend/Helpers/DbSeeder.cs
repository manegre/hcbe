using HcbeApi.Data;
using HcbeApi.Models;
using Microsoft.EntityFrameworkCore;

namespace HcbeApi.Helpers;

public static class DbSeeder
{
    public static void SeedCommunityMemberships(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;
        var plan = context.MembershipPlans.SingleOrDefault(item => item.Id == CommunityMembership.PlanId);
        if (plan == null)
        {
            plan = new MembershipPlan
            {
                Id = CommunityMembership.PlanId,
                Name = "Membre communautaire — Gratuit",
                NameEn = "Community member — Free",
                Description = "Accès gratuit à la communauté, aux services, aux événements et aux ressources du HCBE Canada.",
                DescriptionEn = "Free access to the HCBE Canada community, services, events and resources.",
                AmountCents = 0,
                Currency = "cad",
                BillingMode = CommunityMembership.BillingMode,
                BenefitsJson = "[\"Accès à l’espace membre\",\"Carte de membre numérique\",\"Services et ressources communautaires\",\"Renouvellement annuel gratuit\"]",
                IsActive = true,
                DisplayOrder = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            context.MembershipPlans.Add(plan);
        }

        var existingUserIds = context.MembershipStandings.Select(item => item.UserId).ToHashSet();
        var userIds = context.Users
            .Where(item => item.IsActive && item.MemberId != null && !existingUserIds.Contains(item.Id))
            .Select(item => item.Id)
            .ToList();
        context.MembershipStandings.AddRange(userIds.Select(userId => CommunityMembership.CreateStanding(userId, now)));
        context.SaveChanges();
    }

    public static void SeedPartnersIfEmpty(ApplicationDbContext context)
    {
        if (context.Partners.Any()) return;

        var names = new[]
        {
            "Faso Énergie",
            "Nakomsé Capital",
            "Sahel Logistique",
            "Boréal Assurance",
            "Karité Coopérative",
            "Ouaga Tech",
            "Zongo & Fils",
            "Laurentide Mobilité"
        };

        context.Partners.AddRange(names.Select((name, index) => new Partner
        {
            Name = name,
            NameEn = name,
            AltText = $"Logo de {name}",
            AltTextEn = $"{name} logo",
            IsFeatured = true,
            IsActive = true,
            DisplayOrder = index
        }));
        context.SaveChanges();
    }

    public static void Seed(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        // Ensure database is created and apply migrations
        context.Database.EnsureCreated();
        
        // Create admin user if not exists
        // Predictable development accounts must never be created in production.
        if (environment.IsDevelopment())
        {
            EnsureAdminUserExists(context);
        }
        
        // Check if TeamMembers table exists, if not create it manually
        EnsureTeamMembersTableExists(context);
        
        // Seed documents if none exist
        SeedDocuments(context, environment);

        SeedGrantsIfEmpty(context);
        SeedConsultationsIfEmpty(context);
        SeedStatisticsIfEmpty(context);
        
        // Seed team members if none exist
        if (!context.TeamMembers.Any())
        {
            var teamMembers = new List<TeamMember>
            {
                new TeamMember
                {
                    Name = "Mâ Ouédraogo Diallo",
                    Position = "Co-Présidente en exercice",
                    Region = "National",
                    Zone = "Zone 1",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20businesswoman%20in%20elegant%20formal%20attire%20with%20confident%20smile%2C%20modern%20office%20setting%20with%20natural%20lighting%2C%20representing%20leadership%20and%20community%20engagement%2C%20high%20quality%20portrait%20photography%20with%20professional%20composition%20and%20inspiring%20presence&width=400&height=500&seq=equipe-copresidente-001&orientation=portrait",
                    Bio = "Co-Présidente en exercice du HCBE Canada et Déléguée Titulaire de la Zone 1, avec une expertise reconnue dans le développement communautaire et l'accompagnement des diasporas africaines.",
                    Email = "m.ouedraogo@hcbecanada.org",
                    Order = 1,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Ismaël Aziz Daboné",
                    Position = "Co-Président",
                    Region = "National",
                    Zone = "Zone 2",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20businessman%20in%20formal%20suit%20standing%20confidently%20with%20warm%20smile%2C%20modern%20office%20background%20with%20natural%20lighting%2C%20representing%20leadership%20and%20community%20service%2C%20high%20quality%20portrait%20photography%20with%20professional%20composition%20and%20dignified%20presence&width=400&height=500&seq=equipe-copresident-001&orientation=portrait",
                    Bio = "Co-Président du HCBE Canada et Délégué Titulaire de la Zone 2, spécialiste en gestion de projets communautaires et en mobilisation de la diaspora pour le développement.",
                    Email = "i.dabone@hcbecanada.org",
                    Order = 2,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Ahmed Arnaud Dao",
                    Position = "Responsable à la communication et à la mobilisation",
                    Region = "National",
                    Zone = "Zone 2",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20male%20communications%20specialist%20in%20business%20attire%20with%20friendly%20smile%2C%20modern%20media%20office%20background%20with%20natural%20lighting%2C%20representing%20communication%20expertise%20and%20community%20engagement%2C%20high%20quality%20portrait%20photography%20with%20dynamic%20presence&width=400&height=500&seq=equipe-communication-001&orientation=portrait",
                    Bio = "Responsable à la communication et à la mobilisation, également Délégué Suppléant de la Zone 2, expert en stratégies de communication et engagement communautaire.",
                    Email = "a.dao@hcbecanada.org",
                    Order = 3,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Désiré Kaboré",
                    Position = "Responsable adjoint à la communication et à la mobilisation",
                    Region = "National",
                    Zone = "Zone 1",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20male%20communications%20coordinator%20in%20business%20casual%20attire%20with%20approachable%20smile%2C%20modern%20office%20background%20with%20natural%20lighting%2C%20representing%20media%20relations%20and%20community%20outreach%2C%20high%20quality%20portrait%20photography%20with%20engaging%20presence&width=400&height=500&seq=equipe-communication-adj-001&orientation=portrait",
                    Bio = "Responsable adjoint à la communication et à la mobilisation, passionné par le développement des relations communautaires et la diffusion de l'information.",
                    Email = "d.kabore@hcbecanada.org",
                    Order = 4,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Yves Cédric Nana",
                    Position = "Rapporteur Sompoudbnoma",
                    Region = "National",
                    Zone = "Zone 2",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20male%20secretary%20in%20business%20suit%20with%20professional%20smile%2C%20modern%20administrative%20office%20background%20with%20natural%20lighting%2C%20representing%20documentation%20expertise%20and%20organization%2C%20high%20quality%20portrait%20photography%20with%20efficient%20presence&width=400&height=500&seq=equipe-rapporteur-001&orientation=portrait",
                    Bio = "Rapporteur Sompoudbnoma du HCBE Canada, expert en documentation et gestion administrative des activités du conseil.",
                    Email = "y.nana@hcbecanada.org",
                    Order = 5,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Sory Sacko",
                    Position = "Rapporteur adjoint",
                    Region = "National",
                    Zone = "Zone 1",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20male%20assistant%20secretary%20in%20business%20attire%20with%20confident%20smile%2C%20modern%20office%20background%20with%20natural%20lighting%2C%20representing%20administrative%20support%20and%20record%20keeping%2C%20high%20quality%20portrait%20photography%20with%20reliable%20presence&width=400&height=500&seq=equipe-rapporteur-adj-001&orientation=portrait",
                    Bio = "Rapporteur adjoint, spécialisé dans la gestion documentaire et le suivi des procès-verbaux des réunions du conseil.",
                    Email = "s.sacko@hcbecanada.org",
                    Order = 6,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Ghislain Darga",
                    Position = "Trésorier",
                    Region = "National",
                    Zone = "Zone 2",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20male%20treasurer%20in%20business%20suit%20with%20trustworthy%20smile%2C%20modern%20financial%20office%20background%20with%20natural%20lighting%2C%20representing%20financial%20management%20and%20integrity%2C%20high%20quality%20portrait%20photography%20with%20reliable%20presence&width=400&height=500&seq=equipe-tresorier-002&orientation=portrait",
                    Bio = "Trésorier du HCBE Canada, expert en gestion financière d'organisations à but non lucratif et en planification budgétaire.",
                    Email = "g.darga@hcbecanada.org",
                    Order = 7,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Ismaël Zeba",
                    Position = "Trésorier adjoint",
                    Region = "National",
                    Zone = "Zone 1",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20male%20assistant%20treasurer%20in%20business%20attire%20with%20professional%20smile%2C%20modern%20accounting%20office%20background%20with%20natural%20lighting%2C%20representing%20financial%20support%20and%20budget%20management%2C%20high%20quality%20portrait%20photography%20with%20dependable%20presence&width=400&height=500&seq=equipe-tresorier-adj-001&orientation=portrait",
                    Bio = "Trésorier adjoint et Délégué Suppléant de la Zone 1, spécialisé en comptabilité et contrôle financier.",
                    Email = "i.zeba@hcbecanada.org",
                    Order = 8,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Kady Moné",
                    Position = "Représentante des femmes",
                    Region = "National",
                    Zone = "Zone 1",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20woman%20advocate%20in%20elegant%20business%20attire%20with%20inspiring%20smile%2C%20modern%20community%20office%20background%20with%20natural%20lighting%2C%20representing%20women%20empowerment%20and%20leadership%2C%20high%20quality%20portrait%20photography%20with%20empowering%20presence&width=400&height=500&seq=equipe-femmes-001&orientation=portrait",
                    Bio = "Représentante des femmes au sein du HCBE Canada, engagée dans la promotion des droits et du leadership féminin.",
                    Email = "k.mone@hcbecanada.org",
                    Order = 9,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Mamouna Kaboré",
                    Position = "Représentante adjointe des femmes",
                    Region = "National",
                    Zone = "Zone 2",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20woman%20community%20leader%20in%20business%20casual%20attire%20with%20warm%20smile%2C%20modern%20office%20background%20with%20natural%20lighting%2C%20representing%20women%20advocacy%20and%20support%2C%20high%20quality%20portrait%20photography%20with%20compassionate%20presence&width=400&height=500&seq=equipe-femmes-adj-001&orientation=portrait",
                    Bio = "Représentante adjointe des femmes, dévouée au soutien et à l'autonomisation des femmes burkinabè au Canada.",
                    Email = "m.kabore@hcbecanada.org",
                    Order = 10,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Fawziah Sawadogo",
                    Position = "Représentante des jeunes",
                    Region = "National",
                    Zone = "Zone 1",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20young%20African%20woman%20youth%20leader%20in%20modern%20business%20attire%20with%20energetic%20smile%2C%20contemporary%20office%20background%20with%20natural%20lighting%2C%20representing%20youth%20engagement%20and%20innovation%2C%20high%20quality%20portrait%20photography%20with%20vibrant%20presence&width=400&height=500&seq=equipe-jeunes-001&orientation=portrait",
                    Bio = "Représentante des jeunes au HCBE Canada, passionnée par l'engagement de la jeunesse et le développement des talents.",
                    Email = "f.sawadogo@hcbecanada.org",
                    Order = 11,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Gérard Ouédraogo",
                    Position = "Représentant adjoint des jeunes",
                    Region = "National",
                    Zone = "Zone 2",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20young%20African%20man%20youth%20coordinator%20in%20business%20casual%20attire%20with%20enthusiastic%20smile%2C%20modern%20office%20background%20with%20natural%20lighting%2C%20representing%20youth%20mentorship%20and%20community%20building%2C%20high%20quality%20portrait%20photography%20with%20dynamic%20presence&width=400&height=500&seq=equipe-jeunes-adj-001&orientation=portrait",
                    Bio = "Représentant adjoint des jeunes, engagé dans la mobilisation et l'accompagnement de la jeunesse burkinabè.",
                    Email = "g.ouedraogo@hcbecanada.org",
                    Order = 12,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Julien Tougouri",
                    Position = "Représentant des personnes âgées",
                    Region = "National",
                    Zone = "Zone 1",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20senior%20African%20man%20elder%20representative%20in%20formal%20attire%20with%20wise%20smile%2C%20modern%20office%20background%20with%20natural%20lighting%2C%20representing%20wisdom%20and%20community%20guidance%2C%20high%20quality%20portrait%20photography%20with%20dignified%20presence&width=400&height=500&seq=equipe-aines-001&orientation=portrait",
                    Bio = "Représentant des personnes âgées, gardien des valeurs traditionnelles et conseiller auprès de la communauté.",
                    Email = "j.tougouri@hcbecanada.org",
                    Order = 13,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Bamory Traoré",
                    Position = "Représentant adjoint des personnes âgées",
                    Region = "National",
                    Zone = "Zone 2",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20senior%20African%20man%20elder%20advisor%20in%20business%20attire%20with%20gentle%20smile%2C%20modern%20office%20background%20with%20natural%20lighting%2C%20representing%20experience%20and%20mentorship%2C%20high%20quality%20portrait%20photography%20with%20respectful%20presence&width=400&height=500&seq=equipe-aines-adj-001&orientation=portrait",
                    Bio = "Représentant adjoint des personnes âgées, engagé dans le soutien et la valorisation des aînés de la communauté.",
                    Email = "b.traore@hcbecanada.org",
                    Order = 14,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Hamadou Désiré Salgo",
                    Position = "Commissaire aux comptes",
                    Region = "National",
                    Zone = "Zone 1",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20male%20auditor%20in%20business%20suit%20with%20trustworthy%20smile%2C%20modern%20accounting%20office%20background%20with%20natural%20lighting%2C%20representing%20financial%20oversight%20and%20transparency%2C%20high%20quality%20portrait%20photography%20with%20authoritative%20presence&width=400&height=500&seq=equipe-commissaire-001&orientation=portrait",
                    Bio = "Commissaire aux comptes du HCBE Canada, expert en audit et contrôle de gestion financière.",
                    Email = "h.salgo@hcbecanada.org",
                    Order = 15,
                    IsActive = true
                },
                new TeamMember
                {
                    Name = "Imranou Yaone",
                    Position = "Commissaire aux comptes",
                    Region = "National",
                    Zone = "Zone 2",
                    Photo = "https://readdy.ai/api/search-image?query=Professional%20African%20male%20financial%20auditor%20in%20business%20attire%20with%20professional%20smile%2C%20modern%20audit%20office%20background%20with%20natural%20lighting%2C%20representing%20financial%20control%20and%20compliance%2C%20high%20quality%20portrait%20photography%20with%20meticulous%20presence&width=400&height=500&seq=equipe-commissaire-002&orientation=portrait",
                    Bio = "Commissaire aux comptes, spécialisé en vérification financière et conformité des opérations du conseil.",
                    Email = "i.yaone@hcbecanada.org",
                    Order = 16,
                    IsActive = true
                }
            };

            context.TeamMembers.AddRange(teamMembers);
            context.SaveChanges();
            Console.WriteLine("✓ Seeded 16 team members");
        }
        
        // Seed events if none exist
        SeedEvents(context);
        
        // Seed news if none exist
        SeedNews(context);

        // Seed associations: add all if empty, otherwise add any missing by Name + City
        var seedAssociations = new List<Association>
        {
            new Association {
                Name = "Association Yam Taaba",
                Province = "Alberta",
                City = "Calgary",
                Description = "Association culturelle et sociale dédiée à la promotion de la culture burkinabè et au soutien des membres de la communauté.",
                Domains = new List<string>{ "Culture", "Social", "Éducation" },
                Contact = "contact@yamtaaba.ca",
                Phone = "+1 (403) XXX-XXXX",
                President = "M. Souleymane Ouédraogo",
                MemberCount = "85+",
                FoundedYear = 2015,
                ImageUrl = "https://readdy.ai/api/search-image?query=African%20cultural%20association%20group%20photo%20with%20diverse%20members%20in%20traditional%20attire%20gathered%20in%20modern%20community%20center%2C%20warm%20welcoming%20professional%20lighting%20creating%20sense%20of%20unity%20and%20pride%2C%20simple%20clean%20background&width=600&height=400&seq=assoc-yamtaaba-001&orientation=landscape",
                IsActive = true
            },
            new Association {
                Name = "Burkinabè de Montréal",
                Province = "Québec",
                City = "Montréal",
                Description = "Regroupement des Burkinabè de la région de Montréal pour favoriser l'entraide, l'intégration et le développement communautaire.",
                Domains = new List<string>{ "Intégration", "Entraide", "Développement" },
                Contact = "info@burkinabemtl.org",
                Phone = "+1 (514) XXX-XXXX",
                President = "Mme. Aminata Kaboré",
                MemberCount = "150+",
                FoundedYear = 2012,
                ImageUrl = "https://readdy.ai/api/search-image?query=Montreal%20African%20community%20association%20meeting%20with%20engaged%20members%20discussing%20in%20modern%20community%20space%2C%20bright%20professional%20lighting%20creating%20collaborative%20atmosphere%2C%20simple%20clean%20background&width=600&height=400&seq=assoc-montreal-001&orientation=landscape",
                IsActive = true
            },
            new Association {
                Name = "Association des Étudiants Burkinabè de Toronto",
                Province = "Ontario",
                City = "Toronto",
                Description = "Soutien aux étudiants burkinabè dans leur parcours académique et leur intégration dans le système éducatif canadien.",
                Domains = new List<string>{ "Éducation", "Jeunesse", "Mentorat" },
                Contact = "aebt@outlook.com",
                Phone = "+1 (416) XXX-XXXX",
                President = "M. Ibrahim Sawadogo",
                MemberCount = "120+",
                FoundedYear = 2018,
                ImageUrl = "https://readdy.ai/api/search-image?query=African%20student%20association%20group%20studying%20together%20in%20university%20library%20with%20diverse%20young%20students%20collaborating%20with%20books%20and%20laptops%2C%20bright%20academic%20lighting%20creating%20studious%20atmosphere%2C%20simple%20clean%20background&width=600&height=400&seq=assoc-etudiants-001&orientation=landscape",
                IsActive = true
            },
            new Association {
                Name = "Femmes Burkinabè du Canada",
                Province = "Ontario",
                City = "Ottawa",
                Description = "Association dédiée à l'autonomisation des femmes burkinabè à travers l'entrepreneuriat, l'éducation et le soutien mutuel.",
                Domains = new List<string>{ "Femmes", "Entrepreneuriat", "Autonomisation" },
                Contact = "femmesburkinabe@gmail.com",
                Phone = "+1 (613) XXX-XXXX",
                President = "Mme. Mariam Compaoré",
                MemberCount = "95+",
                FoundedYear = 2016,
                ImageUrl = "https://readdy.ai/api/search-image?query=African%20women%20empowerment%20association%20meeting%20with%20confident%20women%20entrepreneurs%20in%20modern%20business%20center%2C%20warm%20professional%20lighting%20creating%20empowering%20atmosphere%2C%20simple%20clean%20background&width=600&height=400&seq=assoc-femmes-001&orientation=landscape",
                IsActive = true
            },
            new Association {
                Name = "Jeunesse Burkinabè de Vancouver",
                Province = "Colombie-Britannique",
                City = "Vancouver",
                Description = "Mobilisation de la jeunesse burkinabè pour des activités sportives, culturelles et de développement personnel.",
                Domains = new List<string>{ "Jeunesse", "Sport", "Culture" },
                Contact = "jbvancouver@yahoo.ca",
                Phone = "+1 (604) XXX-XXXX",
                President = "M. Abdoul Aziz Diallo",
                MemberCount = "70+",
                FoundedYear = 2019,
                ImageUrl = "https://readdy.ai/api/search-image?query=Young%20African%20community%20group%20engaged%20in%20sports%20and%20cultural%20activities%20in%20outdoor%20Vancouver%20setting%2C%20bright%20energetic%20lighting%20creating%20youthful%20vibrant%20atmosphere%2C%20simple%20clean%20background&width=600&height=400&seq=assoc-jeunesse-001&orientation=landscape",
                IsActive = true
            },
            new Association {
                Name = "Association Solidarité Burkina",
                Province = "Québec",
                City = "Québec",
                Description = "Collecte de fonds et organisation de projets de développement au Burkina Faso dans les domaines de l'éducation et de la santé.",
                Domains = new List<string>{ "Développement", "Santé", "Éducation" },
                Contact = "solidariteburkina@hotmail.com",
                Phone = "+1 (418) XXX-XXXX",
                President = "M. Boureima Zongo",
                MemberCount = "60+",
                FoundedYear = 2014,
                ImageUrl = "https://readdy.ai/api/search-image?query=Solidarity%20association%20organizing%20development%20projects%20with%20volunteers%20packing%20supplies%20in%20community%20center%2C%20warm%20compassionate%20lighting%20creating%20humanitarian%20atmosphere%2C%20simple%20clean%20background&width=600&height=400&seq=assoc-solidarite-001&orientation=landscape",
                IsActive = true
            },
            new Association {
                Name = "Entrepreneurs Burkinabè du Canada",
                Province = "Ontario",
                City = "Toronto",
                Description = "Réseau d'entrepreneurs burkinabè pour le partage d'expériences, le mentorat et le développement des affaires.",
                Domains = new List<string>{ "Entrepreneuriat", "Business", "Réseautage" },
                Contact = "ebc@entrepreneursburkina.ca",
                Phone = "+1 (647) XXX-XXXX",
                President = "M. Moussa Traoré",
                MemberCount = "45+",
                FoundedYear = 2020,
                ImageUrl = "https://readdy.ai/api/search-image?query=African%20business%20entrepreneurs%20networking%20at%20professional%20event%20in%20modern%20business%20venue%2C%20professional%20lighting%20creating%20dynamic%20business%20atmosphere%2C%20simple%20clean%20background&width=600&height=400&seq=assoc-entrepreneurs-001&orientation=landscape",
                IsActive = true
            },
            new Association {
                Name = "Association Culturelle Mossi",
                Province = "Manitoba",
                City = "Winnipeg",
                Description = "Préservation et promotion de la culture Mossi à travers des événements culturels, des cours de langue et des célébrations traditionnelles.",
                Domains = new List<string>{ "Culture", "Langue", "Traditions" },
                Contact = "culturemossi@gmail.com",
                Phone = "+1 (204) XXX-XXXX",
                President = "Mme. Rasmata Ouattara",
                MemberCount = "55+",
                FoundedYear = 2017,
                ImageUrl = "https://readdy.ai/api/search-image?query=Traditional%20African%20cultural%20association%20with%20members%20in%20traditional%20Mossi%20attire%20in%20cultural%20center%2C%20warm%20cultural%20lighting%20creating%20heritage%20atmosphere%2C%20simple%20clean%20background&width=600&height=400&seq=assoc-mossi-001&orientation=landscape",
                IsActive = true
            }
        };

        if (!context.Associations.Any())
        {
            context.Associations.AddRange(seedAssociations);
        }
        else
        {
            foreach (var a in seedAssociations)
            {
                if (!context.Associations.Any(x => x.Name == a.Name && x.City == a.City))
                {
                    context.Associations.Add(a);
                }
            }
        }

        // Seed projects: add all if empty, otherwise add any missing by Title
        var seedProjects = new[]
        {
            new Project {
                Title = "Construction d'une École Primaire à Ouagadougou",
                Location = "Ouagadougou, Burkina Faso",
                Type = "Développement au Burkina",
                Status = "En cours",
                Progress = 65,
                Description = "Construction d'une école primaire de 6 classes pour accueillir 300 élèves dans le quartier périphérique de Tanghin. Le projet inclut également la construction de latrines, un point d'eau et l'équipement en mobilier scolaire.",
                ImageUrl = "https://readdy.ai/api/search-image?query=School%20construction%20project%20in%20African%20village%20with%20workers%20building%20classrooms%20in%20rural%20Burkina%20Faso%20setting%20with%20children%20watching%20excitedly%2C%20bright%20hopeful%20lighting%20creating%20sense%20of%20progress%20and%20education%20development%2C%20simple%20clean%20background&width=800&height=600&seq=projet-ecole-001&orientation=landscape",
                Budget = "150 000 $ CAD",
                FundsRaised = "97 500 $ CAD",
                Beneficiaries = "300 élèves",
                StartDate = new DateTime(2023, 9, 1),
                EndDate = new DateTime(2024, 6, 1),
                Partners = new List<string>{"HCBE Canada", "Ministère de l'Éducation BF", "Mairie de Ouagadougou"},
                IsActive = true
            },
            new Project {
                Title = "Programme de Mentorat Professionnel",
                Location = "Canada (National)",
                Type = "Initiative Locale",
                Status = "Actif",
                Progress = 100,
                Description = "Programme de mentorat connectant des professionnels burkinabè établis avec de nouveaux arrivants pour faciliter leur intégration professionnelle au Canada. Plus de 50 paires mentor-mentoré ont été formées avec un taux de réussite de 85%.",
                ImageUrl = "https://readdy.ai/api/search-image?query=Professional%20mentorship%20program%20with%20African%20mentor%20and%20mentee%20in%20modern%20office%20discussing%20career%20development%2C%20bright%20professional%20lighting%20creating%20supportive%20collaborative%20atmosphere%2C%20simple%20clean%20background&width=800&height=600&seq=projet-mentorat-001&orientation=landscape",
                Budget = "25 000 $ CAD",
                FundsRaised = "25 000 $ CAD",
                Beneficiaries = "100+ personnes",
                StartDate = new DateTime(2023, 1, 1),
                EndDate = null,
                Partners = new List<string>{"HCBE Canada", "Comité RH", "Employeurs partenaires"},
                IsActive = true
            },
            new Project {
                Title = "Centre de Santé Communautaire à Bobo-Dioulasso",
                Location = "Bobo-Dioulasso, Burkina Faso",
                Type = "Développement au Burkina",
                Status = "Planification",
                Progress = 20,
                Description = "Projet de construction d'un centre de santé communautaire équipé pour offrir des soins de base, des consultations prénatales et des services de vaccination. Le centre desservira une population de plus de 5 000 personnes.",
                ImageUrl = "https://readdy.ai/api/search-image?query=Community%20health%20center%20project%20in%20African%20village%20with%20medical%20staff%20and%20patients%20at%20rural%20healthcare%20facility%20with%20basic%20medical%20equipment%2C%20warm%20caring%20lighting%20creating%20sense%20of%20health%20and%20wellbeing%2C%20simple%20clean%20background&width=800&height=600&seq=projet-sante-001&orientation=landscape",
                Budget = "200 000 $ CAD",
                FundsRaised = "40 000 $ CAD",
                Beneficiaries = "5 000+ personnes",
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(2025, 12, 1),
                Partners = new List<string>{"HCBE Canada", "Ministère de la Santé BF", "ONG partenaires"},
                IsActive = true
            },
            new Project {
                Title = "Fonds d'Urgence Communautaire",
                Location = "Canada (National)",
                Type = "Initiative Locale",
                Status = "Actif",
                Progress = 100,
                Description = "Fonds de solidarité pour venir en aide aux membres de la communauté burkinabè en situation d'urgence (décès, maladie grave, catastrophe). Le fonds a déjà aidé 25 familles en difficulté.",
                ImageUrl = "https://readdy.ai/api/search-image?query=Community%20emergency%20fund%20support%20with%20volunteers%20helping%20families%20in%20need%20in%20compassionate%20community%20center%20setting%20with%20people%20receiving%20assistance%2C%20warm%20supportive%20lighting%20creating%20sense%20of%20solidarity%20and%20care%2C%20simple%20clean%20background&width=800&height=600&seq=projet-urgence-001&orientation=landscape",
                Budget = "50 000 $ CAD",
                FundsRaised = "50 000 $ CAD",
                Beneficiaries = "25 familles",
                StartDate = new DateTime(2022, 1, 1),
                EndDate = null,
                Partners = new List<string>{"HCBE Canada", "Comité SONGRÉ", "Donateurs privés"},
                IsActive = true
            },
            new Project {
                Title = "Projet d'Électrification Solaire Rurale",
                Location = "Province du Yatenga, Burkina Faso",
                Type = "Développement au Burkina",
                Status = "En cours",
                Progress = 45,
                Description = "Installation de systèmes solaires dans 10 villages ruraux pour fournir l'électricité aux écoles, centres de santé et foyers. Le projet vise à améliorer les conditions de vie et faciliter l'accès à l'éducation et aux soins.",
                ImageUrl = "https://readdy.ai/api/search-image?query=Solar%20panel%20installation%20project%20in%20rural%20African%20village%20with%20technicians%20working%20on%20rooftops%20in%20sunny%20rural%20Burkina%20Faso%20landscape%20with%20villagers%20watching%2C%20bright%20optimistic%20lighting%20creating%20sense%20of%20sustainable%20development%2C%20simple%20clean%20background&width=800&height=600&seq=projet-solaire-001&orientation=landscape",
                Budget = "180 000 $ CAD",
                FundsRaised = "81 000 $ CAD",
                Beneficiaries = "10 villages",
                StartDate = new DateTime(2023, 6, 1),
                EndDate = new DateTime(2024, 12, 1),
                Partners = new List<string>{"HCBE Canada", "Agence Nationale des Énergies Renouvelables", "Bailleurs internationaux"},
                IsActive = true
            },
            new Project {
                Title = "Programme d'Alphabétisation des Femmes",
                Location = "Canada et Burkina Faso",
                Type = "Développement au Burkina",
                Status = "Actif",
                Progress = 80,
                Description = "Programme d'alphabétisation en français et en langues locales pour les femmes adultes au Burkina Faso, financé et coordonné par la diaspora. Plus de 200 femmes ont déjà bénéficié du programme.",
                ImageUrl = "https://readdy.ai/api/search-image?query=Women%20literacy%20program%20in%20African%20village%20with%20adult%20women%20learning%20to%20read%20and%20write%20in%20classroom%20setting%20with%20teacher%20and%20engaged%20students%2C%20warm%20encouraging%20lighting%20creating%20sense%20of%20empowerment%20and%20education%2C%20simple%20clean%20background&width=800&height=600&seq=projet-alphabetisation-001&orientation=landscape",
                Budget = "35 000 $ CAD",
                FundsRaised = "28 000 $ CAD",
                Beneficiaries = "200+ femmes",
                StartDate = new DateTime(2022, 9, 1),
                EndDate = new DateTime(2024, 9, 1),
                Partners = new List<string>{"HCBE Canada", "Femmes Burkinabè du Canada", "Associations locales BF"},
                IsActive = true
            }
        };

        if (!context.Projects.Any())
        {
            context.Projects.AddRange(seedProjects);
        }
        else
        {
            foreach (var p in seedProjects)
            {
                if (!context.Projects.Any(x => x.Title == p.Title))
                {
                    context.Projects.Add(p);
                }
            }
        }

        context.SaveChanges();
        Console.WriteLine("✓ Seeded base data: Events, Associations, Projects");
    }

    private static void EnsureTeamMembersTableExists(ApplicationDbContext context)
    {
        try
        {
            // Try to query the table to see if it exists
            context.TeamMembers.Any();
        }
        catch
        {
            // If it fails, create the table using raw SQL
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS TeamMembers (
                    Id TEXT NOT NULL CONSTRAINT PK_TeamMembers PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Position TEXT NOT NULL,
                    Region TEXT NOT NULL,
                    Zone TEXT NOT NULL,
                    Photo TEXT NULL,
                    Bio TEXT NULL,
                    Email TEXT NULL,
                    IsActive INTEGER NOT NULL,
                    'Order' INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_TeamMembers_Order ON TeamMembers ('Order');
                CREATE INDEX IF NOT EXISTS IX_TeamMembers_IsActive ON TeamMembers (IsActive);
            ");
            Console.WriteLine("✓ Created TeamMembers table");
        }
    }

    private static void EnsureAdminUserExists(ApplicationDbContext context)
    {
        EnsureSeedAdminUser(
            context,
            email: "admin@hcbe.ca",
            firstName: "Admin",
            lastName: "HCBE");

        EnsureSeedAdminUser(
            context,
            email: "test@hcbe.ca",
            firstName: "Fabrice",
            lastName: "Test");

        context.SaveChanges();
        Console.WriteLine("✓ Admin users ready (admin@hcbe.ca | test@hcbe.ca / hcbe@2025!)");
    }

    private static void EnsureSeedAdminUser(
        ApplicationDbContext context,
        string email,
        string firstName,
        string lastName)
    {
        const string seedPassword = "hcbe@2025!";
        var existingUser = context.Users.FirstOrDefault(u => u.Email == email);

        if (existingUser != null)
        {
            existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword);
            existingUser.IsAdmin = true;
            existingUser.FirstName = firstName;
            existingUser.LastName = lastName;
            return;
        }

        context.Users.Add(new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
            FirstName = firstName,
            LastName = lastName,
            IsAdmin = true
        });
    }

    // Mise à jour du DbSeeder.cs pour inclure les URLs des fichiers
    private static void SeedDocuments(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        if (!context.Documents.Any())
        {
            Console.WriteLine("Seeding documents...");
            
            // Créer le dossier uploads s'il n'existe pas
            var uploadsPath = Path.Combine(environment.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsPath);
            
            // Créer des PDFs factices
            CreateDummyPDF(uploadsPath, "statuts-hcbe-canada.pdf", "Statuts du HCBE Canada");
            CreateDummyPDF(uploadsPath, "reglements-interieurs.pdf", "Règlements Intérieurs");
            CreateDummyPDF(uploadsPath, "code-de-conduite.pdf", "Code de Conduite");
            CreateDummyPDF(uploadsPath, "plan-strategique-2024-2027.pdf", "Plan Stratégique 2024-2027");
            
            var documents = new List<Document>
            {
                new Document
                {
                    Name = "Statuts du HCBE Canada",
                    Description = "Document officiel définissant la structure, les objectifs et le fonctionnement du Haut Conseil des Burkinabè de l'Extérieur au Canada.",
                    Icon = "ri-file-text-line",
                    Category = "officiel",
                    Pages = "24 pages",
                    Size = "2.4 MB",
                    Url = "/uploads/statuts-hcbe-canada.pdf",
                    Type = ".pdf",
                    DisplayOrder = 1,
                    IsActive = true
                },
                new Document
                {
                    Name = "Règlements Intérieurs",
                    Description = "Ensemble des règles et procédures régissant les activités quotidiennes et la gouvernance du comité de base.",
                    Icon = "ri-book-line",
                    Category = "officiel",
                    Pages = "18 pages",
                    Size = "1.8 MB",
                    Url = "/uploads/reglements-interieurs.pdf",
                    Type = ".pdf",
                    DisplayOrder = 2,
                    IsActive = true
                },
                new Document
                {
                    Name = "Code de Conduite",
                    Description = "Principes éthiques et normes de comportement attendus de tous les membres et représentants du HCBE Canada.",
                    Icon = "ri-shield-check-line",
                    Category = "officiel",
                    Pages = "12 pages",
                    Size = "1.2 MB",
                    Url = "/uploads/code-de-conduite.pdf",
                    Type = ".pdf",
                    DisplayOrder = 3,
                    IsActive = true
                },
                new Document
                {
                    Name = "Plan Stratégique 2024-2027",
                    Description = "Vision stratégique et feuille de route pour le développement du HCBE Canada et l'accompagnement de la diaspora.",
                    Icon = "ri-roadmap-line",
                    Category = "officiel",
                    Pages = "36 pages",
                    Size = "3.6 MB",
                    Url = "/uploads/plan-strategique-2024-2027.pdf",
                    Type = ".pdf",
                    DisplayOrder = 4,
                    IsActive = true
                }
            };

            context.Documents.AddRange(documents);
            context.SaveChanges();
            Console.WriteLine("✓ Seeded 4 official documents with dummy PDFs");
        }
        else
        {
            Console.WriteLine("✓ Documents already exist");
        }
    }

    private static void SeedEvents(ApplicationDbContext context)
    {
        if (!context.Events.Any())
        {
            var events = new List<Event>
            {
                new Event
                {
                    Title = "Forum Entrepreneuriat Diaspora 2024",
                    Description = "Rencontre annuelle des entrepreneurs burkinabè du Canada. Au programme: conférences inspirantes, ateliers pratiques sur la création d'entreprise, sessions de réseautage, présentations de success stories, opportunités de mentorat. Un événement incontournable pour développer son réseau professionnel et découvrir les opportunités d'affaires.",
                    Date = new DateTime(2024, 3, 15, 9, 0, 0),
                    Location = "Centre des Congrès de Toronto, 255 Front St W, Toronto, ON M5V 2W6",
                    Type = "En présentiel",
                    Zone = "Zone 1",
                    Capacity = 200,
                    RegistrationDeadline = new DateTime(2024, 3, 10),
                    ImageUrl = "https://images.unsplash.com/photo-1511578314322-379afb476865?w=800",
                    Status = "À venir"
                },
                new Event
                {
                    Title = "Célébration Fête Nationale 2024",
                    Description = "Grande célébration de la Fête Nationale du Burkina Faso (11 décembre). Programme festif avec: cérémonie officielle avec levée des couleurs, spectacles de danse et musique traditionnelle, défilé de mode avec tenues traditionnelles, gastronomie burkinabè (riz gras, tô, poulet bicyclette), expositions artisanales, jeux pour enfants.",
                    Date = new DateTime(2024, 12, 11, 17, 0, 0),
                    Location = "Salle municipale d'Ottawa, 110 Laurier Ave W, Ottawa, ON K1P 1J1",
                    Type = "En présentiel",
                    Zone = "Zone 1",
                    Capacity = 300,
                    RegistrationDeadline = new DateTime(2024, 12, 5),
                    ImageUrl = "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=800",
                    Status = "À venir"
                },
                new Event
                {
                    Title = "Atelier Intégration Nouveaux Arrivants",
                    Description = "Séance d'accompagnement pour les nouveaux arrivants burkinabè au Canada. Thèmes abordés: système de santé canadien (RAMQ, OHIP), recherche d'emploi et reconnaissance des diplômes, logement et droits des locataires, inscription scolaire des enfants, cours de français/anglais gratuits, services bancaires, permis de conduire.",
                    Date = new DateTime(2024, 2, 20, 18, 0, 0),
                    Location = "Bibliothèque centrale de Montréal, 1100 Marie-Anne E, Montréal, QC H2J 2B7",
                    Type = "En présentiel",
                    Zone = "Zone 1",
                    Capacity = 50,
                    RegistrationDeadline = new DateTime(2024, 2, 18),
                    ImageUrl = "https://images.unsplash.com/photo-1524178232363-1fb2b075b655?w=800",
                    Status = "À venir"
                },
                new Event
                {
                    Title = "Webinaire: Investir au Burkina Faso",
                    Description = "Conférence en ligne sur les opportunités d'investissement au Burkina Faso. Intervenants: experts économiques, représentants des chambres de commerce, entrepreneurs ayant réussi. Secteurs porteurs: agriculture et agro-industrie, énergies renouvelables, technologies numériques, immobilier. Questions-réponses en direct.",
                    Date = new DateTime(2024, 4, 10, 19, 0, 0),
                    Location = "En ligne",
                    Type = "Virtuel",
                    MeetingLink = "https://zoom.us/j/example123",
                    Capacity = 500,
                    RegistrationDeadline = new DateTime(2024, 4, 9),
                    ImageUrl = "https://images.unsplash.com/photo-1600880292203-757bb62b4baf?w=800",
                    Status = "À venir"
                },
                new Event
                {
                    Title = "Soirée Culturelle Traditionnelle",
                    Description = "Soirée de célébration de la culture burkinabè avec performances artistiques. Au programme: concert de musique traditionnelle (balafon, djembé, kora), contes et légendes moagas, démonstrations de danses folkloriques, exposition d'art burkinabè, dégustation de mets traditionnels. Tenue traditionnelle encouragée.",
                    Date = new DateTime(2024, 5, 25, 18, 30, 0),
                    Location = "Maison de la culture de Calgary, 225 9 Ave SE, Calgary, AB T2G 0S1",
                    Type = "En présentiel",
                    Zone = "Zone 2",
                    Capacity = 150,
                    RegistrationDeadline = new DateTime(2024, 5, 20),
                    ImageUrl = "https://images.unsplash.com/photo-1533174072545-7a4b6ad7a6c3?w=800",
                    Status = "À venir"
                },
                new Event
                {
                    Title = "Tournoi Football Communautaire",
                    Description = "Tournoi de football amical inter-communautés burkinabè du Canada. Compétition par équipes (8 joueurs + gardien), catégories: seniors, juniors (16-25 ans), vétérans (35+). Inscription par équipe. Prix et trophées pour les gagnants. Buvette et restauration sur place. Ambiance conviviale garantie!",
                    Date = new DateTime(2024, 7, 13, 10, 0, 0),
                    Location = "Parc Jarry, 200 Faillon O, Montréal, QC H2R 2V1",
                    Type = "En présentiel",
                    Zone = "Zone 1",
                    Capacity = 200,
                    RegistrationDeadline = new DateTime(2024, 7, 1),
                    ImageUrl = "https://images.unsplash.com/photo-1579952363873-27f3bade9f55?w=800",
                    Status = "À venir"
                },
                new Event
                {
                    Title = "Remise de Bourses d'Études 2024",
                    Description = "Cérémonie officielle de remise des bourses d'études du HCBE Canada. 10 bourses de 2000$ CAD chacune seront remises aux étudiants burkinabè méritants. Critères: excellence académique, engagement communautaire, projet de développement. Discours inspirants, témoignages d'anciens boursiers, cocktail de réception.",
                    Date = new DateTime(2024, 6, 8, 15, 0, 0),
                    Location = "Université de Toronto, Convocation Hall, 31 King's College Cir, Toronto, ON M5S 1A1",
                    Type = "En présentiel",
                    Zone = "Zone 1",
                    Capacity = 120,
                    RegistrationDeadline = new DateTime(2024, 6, 5),
                    ImageUrl = "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=800",
                    Status = "À venir"
                },
                new Event
                {
                    Title = "Conférence Éducation Financière",
                    Description = "Atelier pratique sur la gestion financière personnelle au Canada. Sujets traités: budget familial et épargne, crédit et cote de crédit, placements (REER, CELI, REEE), assurances (vie, habitation, auto), planification retraite, fiscalité canadienne, envois d'argent au Burkina. Conseiller financier certifié.",
                    Date = new DateTime(2024, 8, 22, 18, 30, 0),
                    Location = "Bibliothèque publique de Vancouver, 350 W Georgia St, Vancouver, BC V6B 6B1",
                    Type = "En présentiel",
                    Zone = "Zone 2",
                    Capacity = 60,
                    RegistrationDeadline = new DateTime(2024, 8, 20),
                    ImageUrl = "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=800",
                    Status = "À venir"
                },
                new Event
                {
                    Title = "Journée Internationale de la Femme",
                    Description = "Célébration de la Journée Internationale des Droits de la Femme avec les femmes burkinabè du Canada. Programme enrichissant: conférences sur le leadership féminin, table ronde sur l'autonomisation économique, ateliers de développement personnel, témoignages de femmes inspirantes, networking, exposition d'entrepreneures burkinabè.",
                    Date = new DateTime(2024, 3, 8, 14, 0, 0),
                    Location = "Centre communautaire de Winnipeg, 123 Main St, Winnipeg, MB R3C 1A3",
                    Type = "En présentiel",
                    Zone = "Zone 2",
                    Capacity = 100,
                    RegistrationDeadline = new DateTime(2024, 3, 5),
                    ImageUrl = "https://images.unsplash.com/photo-1573496359142-b8d87734a5a2?w=800",
                    Status = "À venir"
                },
                new Event
                {
                    Title = "Assemblée Générale Annuelle 2024",
                    Description = "Assemblée Générale Annuelle du HCBE Canada - Présence obligatoire pour les membres. Ordre du jour: rapport d'activités 2023, présentation des comptes et bilan financier, rapport des commissaires aux comptes, élection du nouveau conseil d'administration (mandats 2024-2026), orientations stratégiques, questions diverses. Quorum requis.",
                    Date = new DateTime(2024, 3, 16, 14, 0, 0),
                    Location = "Hôtel Marriott Centre-ville, 475 Atwater Ave, Montréal, QC H3J 2M3",
                    Type = "Hybride",
                    Zone = "National",
                    MeetingLink = "https://zoom.us/j/aga2024",
                    Capacity = 250,
                    RegistrationDeadline = new DateTime(2024, 3, 10),
                    ImageUrl = "https://images.unsplash.com/photo-1511578314322-379afb476865?w=800",
                    Status = "À venir"
                }
            };

            context.Events.AddRange(events);
            context.SaveChanges();
            Console.WriteLine("✓ Seeded 10 events");
        }
        else
        {
            Console.WriteLine("✓ Events already exist");
        }
    }

    private static void SeedNews(ApplicationDbContext context)
    {
        if (!context.News.Any())
        {
            var news = new List<News>
            {
                new News
                {
                    Title = "Nouvelle Procédure de Demande de Passeport Burkinabè",
                    Content = "Le Consulat du Burkina Faso annonce la mise en place d'une nouvelle procédure simplifiée pour la demande de passeport. Les citoyens burkinabè résidant au Canada peuvent désormais soumettre leur demande en ligne via le portail consulaire officiel accessible à l'adresse www.consulat-burkina.ca.\n\nDOCUMENTS REQUIS:\n- Preuve de citoyenneté burkinabè (certificat de nationalité ou ancien passeport)\n- Deux photos d'identité récentes (format passeport, fond blanc)\n- Formulaire de demande dûment complété et signé\n- Copie d'une pièce d'identité canadienne valide (permis de conduire, carte de résident permanent)\n- Justificatif de résidence au Canada (facture récente de services publics)\n- Paiement des frais consulaires (225$ CAD)\n\nPROCÉDURE:\n1. Créer un compte sur le portail consulaire\n2. Remplir le formulaire en ligne et télécharger les documents scannés\n3. Effectuer le paiement en ligne par carte de crédit\n4. Prendre rendez-vous pour la prise d'empreintes digitales\n5. Se présenter au consulat avec les originaux des documents\n\nDÉLAIS:\nLe délai de traitement standard est estimé à 4-6 semaines à compter de la soumission complète du dossier. Un service accéléré (2-3 semaines) est disponible moyennant des frais supplémentaires de 100$ CAD.\n\nHEURES DE SERVICE:\nLe service des passeports est ouvert du mardi au vendredi, de 9h00 à 15h00. Rendez-vous obligatoire.\n\nPour toute question, contactez le Consulat au +1 (514) XXX-XXXX ou par courriel à passeports@consulat-burkina.ca",
                    Excerpt = "Nouvelle procédure simplifiée en ligne pour la demande de passeport burkinabè au Canada",
                    Category = "Communiqué Officiel",
                    Author = "Secrétariat HCBE",
                    PublishedDate = new DateTime(2024, 1, 15),
                    IsPinned = true,
                    Status = "published",
                    ImageUrl = "https://images.unsplash.com/photo-1578575437130-527eed3abbec?w=800"
                },
                new News
                {
                    Title = "Programme de Bourses d'Études 2024",
                    Content = "Le HCBE Canada est fier d'annoncer le lancement de son programme de bourses d'études pour l'année académique 2024. Dans le cadre de sa mission de soutien à l'excellence éducative, le Conseil offre 10 bourses d'une valeur de 2 000$ CAD chacune aux étudiants burkinabè méritants inscrits dans des établissements d'enseignement supérieur canadiens.\n\nCRITÈRES D'ÉLIGIBILITÉ:\n- Être de nationalité burkinabè et résider au Canada\n- Être inscrit à temps plein dans un programme universitaire ou collégial reconnu\n- Avoir une moyenne générale minimale de 3.0/4.3 (ou équivalent B)\n- Démontrer un engagement communautaire auprès de la diaspora burkinabè\n- Présenter un projet concret de contribution au développement du Burkina Faso\n\nDOCUMENTS REQUIS:\n- Formulaire de candidature complété\n- Lettre de motivation (maximum 2 pages)\n- Relevés de notes officiels des deux dernières années\n- Deux lettres de recommandation (dont une d'un professeur)\n- Preuve d'inscription pour l'année académique 2024-2025\n- Copie de la carte de membre HCBE (ou preuve d'adhésion)\n- Description détaillée du projet de développement (5 pages maximum)\n\nCRITÈRES DE SÉLECTION:\n- Excellence académique (40%)\n- Engagement communautaire et leadership (30%)\n- Qualité et faisabilité du projet de développement (20%)\n- Situation financière et besoins (10%)\n\nPROCESSUS:\nLes candidatures doivent être soumises en ligne via le portail du HCBE avant le 31 mars 2024 à 23h59 (heure de l'Est). Les entrevues des candidats présélectionnés auront lieu en avril, et les résultats seront annoncés le 15 mai 2024. La remise officielle des bourses se fera lors d'une cérémonie spéciale en juin.\n\nPour plus d'informations et accéder au formulaire de candidature: www.hcbecanada.org/bourses\nContact: bourses@hcbecanada.org",
                    Excerpt = "10 bourses de 2000$ disponibles pour les étudiants burkinabè au Canada - Date limite: 31 mars 2024",
                    Category = "Éducation",
                    Author = "Comité Éducation",
                    PublishedDate = new DateTime(2024, 1, 10),
                    IsPinned = true,
                    Status = "published",
                    ImageUrl = "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?w=800"
                },
                new News
                {
                    Title = "Assemblée Générale Annuelle 2024",
                    Content = "Le Conseil d'Administration du Haut Conseil des Burkinabè de l'Étranger (HCBE) Canada a l'honneur de convoquer tous les membres en règle à l'Assemblée Générale Annuelle (AGA) qui se tiendra:\n\nDATE: Samedi 16 mars 2024\nHEURE: 14h00 (heure de l'Est)\nLIEU: Hôtel Marriott Centre-ville, Salle de conférence A\nAdresse: 475 Atwater Ave, Montréal, QC H3J 2M3\nMODALITÉ: Participation en présentiel et en ligne (lien Zoom communiqué aux inscrits)\n\nORDRE DU JOUR:\n\n1. OUVERTURE ET VÉRIFICATION DU QUORUM (14h00-14h15)\n   - Mot de bienvenue des Co-Présidents\n   - Présentation de l'ordre du jour\n   - Désignation du secrétaire de séance\n\n2. RAPPORT D'ACTIVITÉS 2023 (14h15-15h00)\n   - Bilan des activités et projets réalisés\n   - Statistiques de participation et d'adhésion\n   - Partenariats établis\n   - Défis rencontrés et solutions apportées\n\n3. RAPPORT FINANCIER 2023 (15h00-15h30)\n   - Présentation des états financiers par le Trésorier\n   - Sources de revenus et dépenses\n   - Situation de trésorerie\n\n4. PAUSE CAFÉ (15h30-15h45)\n\n5. RAPPORT DES COMMISSAIRES AUX COMPTES (15h45-16h00)\n   - Vérification des comptes et conformité\n   - Recommandations\n\n6. ÉLECTIONS DU NOUVEAU CONSEIL (16h00-17h30)\n   - Présentation des candidats\n   - Allocutions des candidats (5 minutes chacun)\n   - Vote électronique et dépouillement\n   - Proclamation des résultats\n\n7. ORIENTATIONS STRATÉGIQUES 2024-2026 (17h30-18h15)\n   - Vision et missions prioritaires\n   - Projets majeurs envisagés\n   - Budget prévisionnel\n\n8. QUESTIONS DIVERSES (18h15-18h45)\n\n9. CLÔTURE (18h45)\n\nMODALITÉS DE PARTICIPATION:\n- Inscription obligatoire avant le 10 mars 2024\n- Être membre à jour de ses cotisations pour voter\n- Présentation de la carte de membre lors de l'enregistrement\n- Vote par procuration possible (formulaire disponible sur le site)\n\nIMPORTANT: Le quorum requis pour la validité de l'AGA est fixé à 50% des membres en règle, soit au moins 150 membres présents ou représentés.\n\nINSCRIPTION: www.hcbecanada.org/aga2024\nINFORMATIONS: aga@hcbecanada.org | +1 (514) XXX-XXXX",
                    Excerpt = "Convocation à l'AGA 2024 - 16 mars à Montréal - Élections du nouveau conseil",
                    Category = "Événement",
                    Author = "Bureau Exécutif",
                    PublishedDate = new DateTime(2024, 1, 5),
                    Status = "published",
                    ImageUrl = "https://images.unsplash.com/photo-1511578314322-379afb476865?w=800"
                },
                new News
                {
                    Title = "Services de Légalisation de Documents",
                    Content = "Le HCBE Canada, en partenariat avec le Consulat Général du Burkina Faso, est heureux d'annoncer la mise en place d'un service permanent de légalisation de documents pour faciliter les démarches administratives de nos compatriotes.\n\nSERVICES PROPOSÉS:\n\n1. AUTHENTIFICATION DE DIPLÔMES\n- Diplômes universitaires canadiens pour usage au Burkina Faso\n- Attestations de réussite et relevés de notes\n- Certificats de formation professionnelle\n- Diplômes d'études secondaires\nTarif: 50$ CAD par document\n\n2. CERTIFICATION DE DOCUMENTS D'ÉTAT CIVIL\n- Actes de naissance canadiens\n- Certificats de mariage\n- Certificats de décès\n- Jugements de divorce\nTarif: 50$ CAD par document\n\n3. LÉGALISATION POUR USAGE AU BURKINA FASO\n- Documents juridiques (procurations, contrats)\n- Documents médicaux et sanitaires\n- Documents d'immigration\n- Traductions certifiées\nTarif: 75$ CAD par document\n\n4. SERVICES ADDITIONNELS\n- Copies certifiées conformes: 25$ CAD\n- Traduction assermentée (français-anglais): à partir de 100$ CAD\n- Service express (48h): supplément de 50$ CAD\n\nPROCÉDURE:\n1. Prendre rendez-vous en ligne ou par téléphone\n2. Se présenter avec les documents originaux et une pièce d'identité\n3. Remplir le formulaire de demande\n4. Effectuer le paiement (comptant, débit, crédit)\n5. Recevoir un reçu avec date de récupération\n\nDÉLAIS:\n- Service standard: 5-7 jours ouvrables\n- Service express: 48 heures (frais supplémentaires)\n- Service urgent (cas exceptionnels): 24 heures (frais doublés)\n\nHORAIRE:\nTous les mardis et jeudis\n10h00 - 12h00 et 14h00 - 15h00\nBureau du HCBE Canada\n1234 rue Sainte-Catherine O, Bureau 500\nMontréal, QC H3G 1P5\n\nRÉSERVATION:\nTéléphone: +1 (514) XXX-XXXX\nCourriel: legalisation@hcbecanada.org\nEn ligne: www.hcbecanada.org/services/legalisation\n\nIMPORTANT:\n- Apporter les documents ORIGINAUX\n- Rendez-vous obligatoire\n- Paiement exigé lors du dépôt\n- Les tarifs sont sujets à révision annuelle\n\nNOTE: Ce service ne remplace pas l'apostille de La Haye pour les documents destinés à d'autres pays que le Burkina Faso.",
                    Excerpt = "Nouveau service de légalisation de documents disponible les mardis et jeudis - Rendez-vous obligatoire",
                    Category = "Service",
                    Author = "Département Services",
                    PublishedDate = new DateTime(2023, 12, 20),
                    Status = "published",
                    ImageUrl = "https://images.unsplash.com/photo-1450101499163-c8848c66ca85?w=800"
                },
                new News
                {
                    Title = "Collecte de Fonds pour les Déplacés Internes",
                    Content = "Face à la situation humanitaire préoccupante au Burkina Faso, le HCBE Canada lance une campagne urgente de collecte de fonds pour venir en aide aux personnes déplacées internes (PDI) qui fuient les violences et se réfugient dans les zones plus sécurisées du pays.\n\nCONTEXTE:\nSelon les dernières données du HCR, plus de 2 millions de Burkinabè ont été contraints de quitter leurs foyers en raison de l'insécurité. Ces familles vivent dans des conditions précaires, sans accès adéquat à la nourriture, l'eau potable, les soins de santé et l'éducation.\n\nOBJECTIF DE LA CAMPAGNE:\nCollecte de 50 000$ CAD d'ici le 31 mars 2024\n\nUTILISATION DES FONDS:\n- 40% : Kits alimentaires (riz, huile, sucre, lait, conserves)\n- 25% : Articles d'hygiène et de santé (savon, désinfectant, médicaments de base)\n- 20% : Fournitures scolaires pour les enfants déplacés\n- 10% : Couvertures et articles de première nécessité\n- 5% : Frais de transfert et d'acheminement\n\nCOMMENT CONTRIBUER:\n\n1. VIREMENT INTERAC:\ncourriel: dons@hcbecanada.org\nQuestion: SOLIDARITÉ\nRéponse: BURKINA\n\n2. VIREMENT BANCAIRE:\nBanque: TD Canada Trust\nNom du compte: HCBE Canada - Fonds Humanitaire\nNuméro de compte: XXXXX-XXXXXXX\nNuméro d'institution: XXX\nNuméro de transit: XXXXX\n\n3. PAR CHÈQUE:\nÀ l'ordre de: HCBE Canada\nMention: Fonds Déplacés Internes\nAdresse: 1234 rue Sainte-Catherine O, Bureau 500, Montréal, QC H3G 1P5\n\n4. EN LIGNE:\nwww.hcbecanada.org/dons-deplaces\nPaiement sécurisé par carte de crédit\n\nREÇUS FISCAUX:\nDes reçus d'impôt seront émis pour tous les dons de 20$ et plus. Le HCBE Canada est un organisme enregistré auprès de l'ARC.\n\nTRANSPARENCE:\nUn rapport détaillé de l'utilisation des fonds sera publié sur notre site web et communiqué à tous les donateurs.\n\nPARTENAIRES SUR LE TERRAIN:\nLes fonds seront acheminés via nos partenaires fiables sur place:\n- Croix-Rouge burkinabè\n- Caritas Burkina\n- Association Tin Tua\n\nMOBILISATION:\nNous invitons tous les membres de la diaspora burkinabè, les associations partenaires, les amis du Burkina Faso et toutes les personnes de bonne volonté à contribuer généreusement à cet élan de solidarité.\n\nChaque don, aussi modeste soit-il, peut faire la différence dans la vie d'une famille déplacée. Ensemble, montrons notre solidarité avec nos compatriotes en détresse!\n\nINFORMATIONS:\nTéléphone: +1 (514) XXX-XXXX\nCourriel: solidarite@hcbecanada.org\n\n#SolidaritéBurkina #EnsemblePourLesFamilles #HCBECanada",
                    Excerpt = "Campagne de collecte de 50000$ pour aider les familles déplacées au Burkina Faso",
                    Category = "Solidarité",
                    Author = "Comité Solidarité",
                    PublishedDate = new DateTime(2023, 12, 15),
                    Status = "published",
                    ImageUrl = "https://images.unsplash.com/photo-1532629345422-7515f3d16bb6?w=800"
                },
                new News
                {
                    Title = "Atelier: Entrepreneuriat et Création d'Entreprise au Canada",
                    Content = "Le Comité Entrepreneuriat du HCBE Canada, en collaboration avec Futurpreneur Canada et la Chambre de Commerce du Burkina Faso au Canada, organise un atelier pratique intensif sur l'entrepreneuriat et la création d'entreprise au Canada.\n\nDATE ET LIEU:\nSamedi 27 janvier 2024\n13h00 - 17h00\nCentre d'entrepreneurship McGill\n3501 Peel Street, Montréal, QC H3A 1X1\n\nPUBLIC CIBLE:\n- Entrepreneurs en devenir\n- Porteurs de projets d'affaires\n- Professionnels souhaitant se lancer à leur compte\n- Étudiants intéressés par l'entrepreneuriat\n\nPROGRAMME DÉTAILLÉ:\n\n13h00 - 13h30: ACCUEIL ET RÉSEAUTAGE\n- Inscription et remise de documentation\n- Café et collations\n- Session de networking\n\n13h30 - 14h15: MODULE 1 - STRUCTURES JURIDIQUES\nAnimé par Me Salif Ouédraogo, avocat d'affaires\n- Entreprise individuelle vs incorporation\n- Avantages et inconvénients de chaque structure\n- Aspects fiscaux et légaux\n- Choix du nom et enregistrement\n\n14h15 - 15h00: MODULE 2 - FINANCEMENT ET SUBVENTIONS\nAnimé par Mme Sophie Kaboré, conseillère Futurpreneur\n- Sources de financement disponibles\n- Subventions gouvernementales (fédéral et provincial)\n- Prêts aux entrepreneurs\n- Micro-crédit et financement alternatif\n- Capital de risque et investisseurs providentiels\n\n15h00 - 15h15: PAUSE-CAFÉ\n\n15h15 - 16h00: MODULE 3 - PLAN D'AFFAIRES ET STRATÉGIE\nAnimé par M. Ahmed Dao, entrepreneur à succès\n- Éléments clés d'un plan d'affaires\n- Étude de marché et analyse de la concurrence\n- Stratégie de marketing et positionnement\n- Projections financières\n\n16h00 - 16h45: MODULE 4 - FISCALITÉ ET COMPTABILITÉ\nAnimé par M. Ibrahim Traoré, CPA\n- Obligations fiscales de l'entrepreneur\n- TPS/TVQ: comprendre et gérer\n- Déductions et crédits d'impôt disponibles\n- Tenue de livres et logiciels comptables\n\n16h45 - 17h00: QUESTIONS-RÉPONSES ET CLÔTURE\n\nBONUS:\nChaque participant recevra:\n- Guide pratique \"Démarrer son entreprise au Canada\"\n- Liste de ressources et organismes d'aide\n- 30 minutes de consultation gratuite avec un conseiller\n- Accès au réseau des entrepreneurs HCBE\n\nCONFÉRENCIERS INVITÉS - SUCCESS STORIES:\nTémoignages inspirants de:\n- Fatoumata Sawadogo, fondatrice de \"Délices d'Afrique\" (restauration)\n- Moussa Compaoré, CEO de \"Tech Solutions BF\" (IT)\n- Aminata Zongo, propriétaire de \"Beaut'Essence\" (cosmétiques)\n\nINSCRIPTION:\nGRATUITE mais OBLIGATOIRE\nPlaces limitées à 60 participants\nFormulaire en ligne: www.hcbecanada.org/atelier-entrepreneuriat\nDate limite: 24 janvier 2024\n\nINFORMATIONS:\nCourriel: entrepreneuriat@hcbecanada.org\nTéléphone: +1 (514) XXX-XXXX\n\nIMPORTANT:\n- Apporter un ordinateur portable (si possible)\n- Prendre de quoi noter\n- Préparer vos questions\n- Arriver 15 minutes avant le début\n\nCet atelier est une occasion unique de recevoir des conseils d'experts, de rencontrer d'autres entrepreneurs burkinabè et de faire les premiers pas vers la réalisation de votre projet d'entreprise!\n\nNe manquez pas cette opportunité!",
                    Excerpt = "Atelier intensif gratuit sur la création d'entreprise au Canada - 27 janvier 2024 à Montréal",
                    Category = "Formation",
                    Author = "Comité Entrepreneuriat",
                    PublishedDate = new DateTime(2023, 12, 10),
                    Status = "published",
                    ImageUrl = "https://images.unsplash.com/photo-1542744173-8e7e53415bb0?w=800"
                },
                new News
                {
                    Title = "Nouvelle Section Jeunesse du HCBE",
                    Content = "Le HCBE Canada est fier d'annoncer la création officielle de sa Section Jeunesse, une initiative ambitieuse visant à mobiliser, impliquer et valoriser la jeunesse burkinabè du Canada dans la vie communautaire et le développement du Burkina Faso.\n\nVISION:\nDevenir le principal catalyseur de l'engagement de la jeunesse burkinabè au Canada, en créant un espace dynamique d'expression, d'action et de développement personnel et collectif.\n\nMISSION:\n- Favoriser l'engagement civique des jeunes Burkinabè\n- Préserver et promouvoir le patrimoine culturel burkinabè\n- Créer des opportunités de mentorat et de réseautage\n- Encourager l'excellence académique et professionnelle\n- Développer des projets de développement au Burkina Faso\n\nQUI PEUT ADHÉRER:\nTous les jeunes de 16 à 35 ans, résidant au Canada, et:\n- De nationalité burkinabè, OU\n- D'origine burkinabè (au moins un parent burkinabè), OU\n- Intéressés par la culture et le développement du Burkina Faso\n\nCOTISATION ANNUELLE:\n- Étudiants: 20$ CAD\n- Jeunes professionnels: 30$ CAD\n(Inscription gratuite la première année pour les membres fondateurs)\n\nSTRUCTURE:\nComité Exécutif:\n- Coordonnateur(trice) Général(e)\n- Vice-Coordonnateur(trice)\n- Secrétaire\n- Trésorier(ère)\n- Responsable Communication et Médias Sociaux\n\nCommissions Thématiques:\n1. Éducation et Formation\n2. Culture et Patrimoine\n3. Entrepreneuriat et Emploi\n4. Sport et Loisirs\n5. Solidarité et Développement\n\nAXES D'INTERVENTION:\n\n1. ÉDUCATION\n- Programme de tutorat et mentorat\n- Bourses d'études\n- Aide à l'orientation scolaire et professionnelle\n- Ateliers de développement de compétences\n\n2. CULTURE\n- Organisation d'événements culturels jeunes\n- Cours de langues nationales (mooré, dioula, fulfuldé)\n- Ateliers de danse et musique traditionnelle\n- Concours de talents\n\n3. ENTREPRENEURIAT\n- Incubateur de projets jeunes\n- Formation en entrepreneuriat\n- Mise en réseau avec entrepreneurs établis\n- Accompagnement de projets startup\n\n4. DÉVELOPPEMENT\n- Projets de développement communautaire au Burkina\n- Collectes de fonds ciblées\n- Volontariat et stages au Burkina Faso\n- Partenariats avec ONG locales\n\n5. RÉSEAUTAGE\n- Rencontres mensuelles (virtuelles et présentielles)\n- Plateforme de discussion en ligne\n- Annuaire des membres\n- Événements de networking\n\nPROJETS PILOTES 2024:\n- Lancement d'un podcast \"Voix de la Jeunesse Burkinabè\"\n- Organisation d'un hackathon technologique\n- Tournoi sportif inter-provinces\n- Campagne de collecte de livres pour écoles burkinabè\n- Mentorat de 50 jeunes nouvellement arrivés\n\nASSEMBLÉE CONSTITUTIVE:\nPremière réunion officielle\nDate: Samedi 3 février 2024, 15h00\nLieu: En ligne (Zoom) et présentiel à Montréal\nOrdre du jour:\n- Présentation du projet et des objectifs\n- Adoption des statuts et règlements\n- Élection du comité exécutif provisoire\n- Planification des activités 2024\n- Questions diverses\n\nCOMMENT S'IMPLIQUER:\n1. Devenir membre\n2. Rejoindre une commission thématique\n3. Proposer un projet ou une activité\n4. Faire du bénévolat\n5. Participer aux événements\n\nINSCRIPTION:\nFormulaire en ligne: www.hcbecanada.org/jeunesse\nCourriel: jeunesse@hcbecanada.org\nWhatsApp: Groupe \"HCBE Jeunesse Canada\" (lien sur demande)\n\nRÉSEAUX SOCIAUX:\nInstagram: @hcbejeunesse\nFacebook: HCBE Canada Jeunesse\nTikTok: @hcbecanada_jeunes\n\nCette section est VOTRE espace! Venez avec vos idées, votre énergie et votre passion pour bâtir ensemble une communauté jeune, dynamique et engagée!\n\nLa jeunesse burkinabè du Canada a un rôle crucial à jouer dans le présent et l'avenir de notre communauté et de notre pays. Rejoignez-nous!\n\n#JeunesseBurkinabè #HCBEJeunesse #Ensemble #Avenir",
                    Excerpt = "Création de la Section Jeunesse du HCBE - Première réunion le 3 février 2024",
                    Category = "Annonce",
                    Author = "Coordination Jeunesse",
                    PublishedDate = new DateTime(2023, 12, 5),
                    Status = "published",
                    ImageUrl = "https://images.unsplash.com/photo-1529156069898-49953e39b3ac?w=800"
                },
                new News
                {
                    Title = "Partenariat avec Immigration Canada",
                    Content = "Le HCBE Canada est fier d'annoncer la signature d'un protocole d'accord historique avec Immigration, Réfugiés et Citoyenneté Canada (IRCC), marquant une étape importante dans l'accompagnement et l'intégration des nouveaux arrivants burkinabè au Canada.\n\nOBJECTIF DU PARTENARIAT:\nFaciliter l'intégration socio-économique des immigrants burkinabè et renforcer les liens entre la communauté burkinabè et les institutions canadiennes.\n\nDURÉE:\nProtocole d'une durée de 3 ans (2024-2027), renouvelable selon évaluation des résultats.\n\nSERVICES OFFERTS:\n\n1. SÉANCES D'INFORMATION SUR L'IMMIGRATION\nThèmes abordés:\n- Parcours d'immigration et statuts (résident permanent, citoyen)\n- Parrainage familial: procédures et délais\n- Demandes de citoyenneté canadienne\n- Droits et obligations des immigrants\n- Réunification familiale\n- Statut de réfugié et protection\n\nFréquence: Sessions mensuelles (dernier samedi du mois)\nFormat: Présentiel et virtuel (simultané)\nLangues: Français, anglais et mooré (traduction disponible)\n\n2. AIDE À LA RECHERCHE D'EMPLOI\n- Ateliers d'adaptation de CV au marché canadien\n- Préparation aux entrevues d'embauche\n- Reconnaissance des diplômes et compétences étrangères\n- Équivalences professionnelles et ordres professionnels\n- Ressources pour recherche d'emploi (Job Bank, Guichet-Emplois)\n- Réseautage professionnel\n\nServices spécialisés:\n- Évaluation des compétences professionnelles\n- Plan de carrière personnalisé\n- Coaching individuel (sur rendez-vous)\n- Banque d'offres d'emploi\n\n3. COURS DE LANGUES\nFRANÇAIS (LINC - Language Instruction for Newcomers)\n- Niveaux débutant à avancé\n- Cours de jour et de soir\n- Français des affaires\n- Préparation au TEF Canada\n\nANGLAIS (ESL - English as a Second Language)\n- Tous niveaux\n- Anglais conversationnel\n- Anglais professionnel\n- Préparation à l'IELTS\n\nModalités:\n- Gratuit pour les résidents permanents et réfugiés\n- Évaluation du niveau avant inscription\n- Classes de 15-20 étudiants maximum\n- Matériel pédagogique fourni\n\n4. ACCOMPAGNEMENT ADMINISTRATIF\nSoutien dans les démarches:\n- Obtention du Numéro d'Assurance Sociale (NAS)\n- Demande de carte santé provinciale\n- Ouverture de compte bancaire\n- Demande de permis de conduire\n- Inscription scolaire des enfants\n- Allocations familiales (Allocation canadienne pour enfants)\n- Déclaration de revenus pour nouveaux arrivants\n\n5. AGENT IRCC SUR PLACE\nUn agent d'Immigration Canada sera disponible dans nos bureaux:\n- Fréquence: Premier mercredi de chaque mois\n- Horaire: 10h00 - 16h00\n- Services: Consultations individuelles (30 minutes)\n- Rendez-vous: Obligatoire (réservation en ligne)\n\nL'agent pourra:\n- Répondre aux questions sur les statuts d'immigration\n- Fournir des informations sur les programmes d'immigration\n- Orienter vers les ressources appropriées\n- Clarifier des questions administratives\n- Expliquer les procédures et délais\n\nIMPORTANT: L'agent ne traite PAS les dossiers individuels sur place.\n\nLIEU DES SERVICES:\nBureau Principal du HCBE Canada\n1234 rue Sainte-Catherine Ouest, Bureau 500\nMontréal, Québec H3G 1P5\n\nBureaux Satellites:\n- Toronto: 456 Yonge Street, Suite 300\n- Ottawa: 789 Bank Street, Bureau 200\n- Calgary: 321 8th Avenue SW, Suite 150\n\nCOMMENT BÉNÉFICIER DES SERVICES:\n\n1. Inscription en ligne: www.hcbecanada.org/integration\n2. Remplir le formulaire d'évaluation des besoins\n3. Recevoir un plan d'accompagnement personnalisé\n4. Réserver ses places aux ateliers et cours\n\nÉLIGIBILITÉ:\n- Résidents permanents\n- Réfugiés acceptés\n- Demandeurs d'asile\n- Détenteurs de permis de travail ou d'études (services limités)\n\nDOCUMENTS REQUIS:\n- Preuve de statut au Canada (carte RP, lettre de confirmation)\n- Pièce d'identité avec photo\n- Preuve d'origine burkinabè (passeport, acte de naissance)\n\nCONTACT:\nCoordonnateur Programme d'Intégration\nTéléphone: +1 (514) XXX-XXXX (poste 3)\nCourriel: integration@hcbecanada.org\n\nHEURES D'OUVERTURE:\nLundi - Vendredi: 9h00 - 17h00\nSamedi: 10h00 - 14h00 (sur rendez-vous)\n\nCe partenariat représente une avancée majeure pour notre communauté. Il témoigne de la reconnaissance par le gouvernement canadien du rôle important du HCBE dans l'accueil et l'intégration des nouveaux arrivants burkinabè.\n\nNous invitons tous nos compatriotes nouvellement arrivés ou en processus d'établissement à profiter de ces services gratuits pour faciliter leur intégration et réussir leur parcours au Canada.\n\nBienvenue chez vous!",
                    Excerpt = "Signature d'un accord avec IRCC pour faciliter l'intégration des nouveaux arrivants burkinabè",
                    Category = "Partenariat",
                    Author = "Direction HCBE",
                    PublishedDate = new DateTime(2023, 11, 28),
                    Status = "published",
                    ImageUrl = "https://images.unsplash.com/photo-1521791055366-0d553872125f?w=800"
                }
            };

            context.News.AddRange(news);
            context.SaveChanges();
            Console.WriteLine("✓ Seeded 8 news articles");
        }
        else
        {
            Console.WriteLine("✓ News already exist");
        }
    }

    private static void CreateDummyPDF(string uploadsPath, string fileName, string title)
    {
        var filePath = Path.Combine(uploadsPath, fileName);
        
        // Ne créer que si le fichier n'existe pas déjà
        if (!File.Exists(filePath))
        {
            // Créer un PDF basique avec du texte
            var pdfContent = $@"%PDF-1.4
    1 0 obj
    <<
    /Type /Catalog
    /Pages 2 0 R
    >>
    endobj

    2 0 obj
    <<
    /Type /Pages
    /Kids [3 0 R]
    /Count 1
    >>
    endobj

    3 0 obj
    <<
    /Type /Page
    /Parent 2 0 R
    /MediaBox [0 0 612 792]
    /Contents 4 0 R
    /Resources <<
    /Font <<
    /F1 5 0 R
    >>
    >>
    >>
    endobj

    4 0 obj
    <<
    /Length 100
    >>
    stream
    BT
    /F1 24 Tf
    50 700 Td
    ({title}) Tj
    ET
    BT
    /F1 12 Tf
    50 650 Td
    (Document factice - À remplacer par le document réel) Tj
    ET
    endstream
    endobj

    5 0 obj
    <<
    /Type /Font
    /Subtype /Type1
    /BaseFont /Helvetica
    >>
    endobj

    xref
    0 6
    0000000000 65535 f 
    0000000009 00000 n 
    0000000058 00000 n 
    0000000115 00000 n 
    0000000274 00000 n 
    0000000423 00000 n 
    trailer
    <<
    /Size 6
    /Root 1 0 R
    >>
    startxref
    521
    %%EOF";
            
            File.WriteAllText(filePath, pdfContent);
            Console.WriteLine($"  Created dummy PDF: {fileName}");
        }
    }

    public static void SeedGrantsIfEmpty(ApplicationDbContext context)
    {
        if (context.GrantPrograms.Any())
        {
            return;
        }

        Console.WriteLine("Seeding grant programs...");

        var grants = new List<GrantProgram>
        {
            new()
            {
                Title = "Bourses d'Études Supérieures",
                Description = "Programmes de bourses pour les étudiants burkinabè souhaitant poursuivre des études universitaires au Canada.",
                Icon = "ri-graduation-cap-line",
                Amount = "Jusqu'à 15 000 $ CAD",
                Duration = "Annuel",
                EligibilityCriteria = new List<string>
                {
                    "Être de nationalité burkinabè",
                    "Avoir une admission dans une université canadienne",
                    "Démontrer l'excellence académique",
                    "Présenter un projet d'études pertinent",
                },
                DisplayOrder = 1,
            },
            new()
            {
                Title = "Formations Professionnelles",
                Description = "Accès à des formations certifiantes pour améliorer vos compétences et faciliter votre intégration professionnelle.",
                Icon = "ri-briefcase-line",
                Amount = "Jusqu'à 5 000 $ CAD",
                Duration = "Selon le programme",
                EligibilityCriteria = new List<string>
                {
                    "Être membre du HCBE Canada",
                    "Avoir un projet professionnel défini",
                    "Démontrer la pertinence de la formation",
                    "S'engager à partager les connaissances acquises",
                },
                DisplayOrder = 2,
            },
            new()
            {
                Title = "Subventions pour Projets Communautaires",
                Description = "Financement pour des initiatives visant le développement de la communauté burkinabè au Canada ou au Burkina Faso.",
                Icon = "ri-community-line",
                Amount = "Jusqu'à 10 000 $ CAD",
                Duration = "Par projet",
                EligibilityCriteria = new List<string>
                {
                    "Projet à impact communautaire mesurable",
                    "Budget détaillé et réaliste",
                    "Équipe de gestion compétente",
                    "Plan de suivi et d'évaluation",
                },
                DisplayOrder = 3,
            },
            new()
            {
                Title = "Programme de Mentorat Entrepreneurial",
                Description = "Accompagnement et soutien financier pour les entrepreneurs burkinabè souhaitant démarrer ou développer leur entreprise.",
                Icon = "ri-rocket-line",
                Amount = "Jusqu'à 8 000 $ CAD",
                Duration = "12 mois",
                EligibilityCriteria = new List<string>
                {
                    "Avoir un plan d'affaires solide",
                    "Démontrer le potentiel de croissance",
                    "Accepter le mentorat du comité",
                    "S'engager dans le programme complet",
                },
                DisplayOrder = 4,
            },
            new()
            {
                Title = "Bourses de Recherche",
                Description = "Soutien pour les chercheurs travaillant sur des thématiques liées au développement du Burkina Faso.",
                Icon = "ri-microscope-line",
                Amount = "Jusqu'à 12 000 $ CAD",
                Duration = "6 à 18 mois",
                EligibilityCriteria = new List<string>
                {
                    "Projet de recherche approuvé",
                    "Pertinence pour le développement du Burkina",
                    "Affiliation à une institution reconnue",
                    "Engagement de publication des résultats",
                },
                DisplayOrder = 5,
            },
            new()
            {
                Title = "Aide à la Mobilité Professionnelle",
                Description = "Support financier pour faciliter la mobilité professionnelle des membres à travers le Canada.",
                Icon = "ri-map-pin-user-line",
                Amount = "Jusqu'à 3 000 $ CAD",
                Duration = "Ponctuel",
                EligibilityCriteria = new List<string>
                {
                    "Offre d'emploi confirmée",
                    "Nécessité de relocalisation",
                    "Être membre actif du HCBE",
                    "Démontrer le besoin financier",
                },
                DisplayOrder = 6,
            },
        };

        context.GrantPrograms.AddRange(grants);
        context.SaveChanges();
        Console.WriteLine($"✓ Seeded {grants.Count} grant programs");
    }

    public static void SeedStatisticsIfEmpty(ApplicationDbContext context)
    {
        if (context.Statistics.Any()) return;

        var now = DateTime.UtcNow;
        context.Statistics.AddRange(
            new Statistic { Key = "provinces", Value = "11", Label = "Provinces et territoires", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            new Statistic { Key = "zones", Value = "2", Label = "Zones de représentation", DisplayOrder = 2, CreatedAt = now, UpdatedAt = now },
            new Statistic { Key = "associations", Value = "15", Label = "Associations répertoriées", DisplayOrder = 3, CreatedAt = now, UpdatedAt = now },
            new Statistic { Key = "membership", Value = "free", Label = "Adhésion gratuite", DisplayOrder = 4, CreatedAt = now, UpdatedAt = now });
        context.SaveChanges();
    }

    public static void SeedConsultationsIfEmpty(ApplicationDbContext context)
    {
        if (context.Consultations.Any())
        {
            return;
        }

        Console.WriteLine("Seeding consultations...");

        var consultations = new List<Consultation>
        {
            new()
            {
                Title = "Sondage annuel 2024",
                Description = "Participez à notre sondage annuel pour nous aider à mieux comprendre vos besoins et améliorer nos services. Vos réponses sont confidentielles et nous permettront de mieux vous servir.",
                Icon = "ri-questionnaire-line",
                LayoutType = "featured",
                ActionUrl = "/contact",
                ActionLabel = "Participer",
                SecondaryActionUrl = "/actualites/annonces",
                SecondaryActionLabel = "Voir les annonces",
                AccentColor = "emerald",
                DisplayOrder = 0,
            },
            new()
            {
                Title = "Boîte à suggestions",
                Description = "Vous avez une idée pour améliorer nos services ou nos activités ? Partagez-la avec nous de manière anonyme ou identifiée.",
                Icon = "ri-feedback-line",
                LayoutType = "card",
                ActionUrl = "/contact",
                ActionLabel = "Soumettre une suggestion",
                AccentColor = "emerald",
                DisplayOrder = 1,
            },
            new()
            {
                Title = "Consultations publiques",
                Description = "Participez aux consultations publiques sur les projets et décisions importantes qui concernent notre communauté.",
                Icon = "ri-discuss-line",
                LayoutType = "card",
                ActionUrl = "/actualites/evenements",
                ActionLabel = "Voir les consultations",
                AccentColor = "amber",
                DisplayOrder = 2,
            },
        };

        context.Consultations.AddRange(consultations);
        context.SaveChanges();
        Console.WriteLine($"✓ Seeded {consultations.Count} consultations");
    }
}
