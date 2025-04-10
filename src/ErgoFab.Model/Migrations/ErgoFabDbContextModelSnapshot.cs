﻿// <auto-generated />
using System;
using ErgoFab.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace ErgoFab.Model.Migrations
{
    [DbContext(typeof(ErgoFabDbContext))]
    partial class ErgoFabDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("ErgoFab.Model.Country", b =>
                {
                    b.Property<short>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("smallint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<short>("Id"));

                    b.Property<string>("EnglishName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<byte[]>("Flag")
                        .HasColumnType("varbinary(max)");

                    b.Property<string>("LocalName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Country");
                });

            modelBuilder.Entity("ErgoFab.Model.Customer", b =>
                {
                    b.Property<int>("CustomerId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("CustomerId"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("Surname")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.HasKey("CustomerId");

                    b.ToTable("TheCustomer");
                });

            modelBuilder.Entity("ErgoFab.Model.Department", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("HeadId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("OrganizationId")
                        .HasColumnType("int");

                    b.Property<int?>("ParentId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("HeadId");

                    b.HasIndex("OrganizationId");

                    b.HasIndex("ParentId");

                    b.ToTable("Department");
                });

            modelBuilder.Entity("ErgoFab.Model.Employee", b =>
                {
                    b.Property<int>("EmpId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("EmpId"));

                    b.Property<short?>("CountryId")
                        .HasColumnType("smallint");

                    b.Property<int>("DepartmentId")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<int>("OrganizationId")
                        .HasColumnType("int");

                    b.Property<string>("SurName")
                        .IsRequired()
                        .HasMaxLength(400)
                        .HasColumnType("nvarchar(400)");

                    b.HasKey("EmpId");

                    b.HasIndex("CountryId");

                    b.HasIndex("DepartmentId");

                    b.HasIndex("OrganizationId");

                    b.ToTable("Employee");
                });

            modelBuilder.Entity("ErgoFab.Model.Expert", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(300)
                        .HasColumnType("nvarchar(300)");

                    b.Property<string>("Surname")
                        .IsRequired()
                        .HasMaxLength(400)
                        .HasColumnType("nvarchar(400)");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasMaxLength(5)
                        .HasColumnType("nvarchar(5)");

                    b.HasKey("Id");

                    b.ToTable("Expert");
                });

            modelBuilder.Entity("ErgoFab.Model.Industry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Description")
                        .IsRequired()
                        .HasMaxLength(1024)
                        .HasColumnType("nvarchar(1024)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.ToTable("Industry");
                });

            modelBuilder.Entity("ErgoFab.Model.Occupation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("EmployeeId")
                        .HasColumnType("int");

                    b.Property<int>("ProjectId")
                        .HasColumnType("int");

                    b.Property<string>("Role")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("EmployeeId");

                    b.HasIndex("ProjectId");

                    b.ToTable("Occupation");
                });

            modelBuilder.Entity("ErgoFab.Model.Organization", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<short?>("CountryId")
                        .HasColumnType("smallint");

                    b.Property<int?>("DirectorId")
                        .HasColumnType("int");

                    b.Property<string>("Title")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("CountryId");

                    b.HasIndex("DirectorId");

                    b.ToTable("Organization");

#if NET7_0_OR_GREATER
                    b.UseTptMappingStrategy();
#endif
                });

            modelBuilder.Entity("ErgoFab.Model.Project", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("IdCustomer")
                        .HasColumnType("int");

                    b.Property<string>("ProjectName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.HasIndex("IdCustomer");

                    b.ToTable("Project");
                });

            modelBuilder.Entity("ExpertIndustry", b =>
                {
                    b.Property<int>("TheExpertsId")
                        .HasColumnType("int");

                    b.Property<int>("TheIndustriesId")
                        .HasColumnType("int");

                    b.HasKey("TheExpertsId", "TheIndustriesId");

                    b.HasIndex("TheIndustriesId");

                    b.ToTable("ExpertIndustry");
                });

            modelBuilder.Entity("ErgoFab.Model.RegionalDivision", b =>
                {
                    b.HasBaseType("ErgoFab.Model.Organization");

                    b.Property<int>("IdParent")
                        .HasColumnType("int");

                    b.Property<string>("RegionDescription")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasIndex("IdParent");

                    b.ToTable("RegionalDivision");
                });

            modelBuilder.Entity("ErgoFab.Model.Department", b =>
                {
                    b.HasOne("ErgoFab.Model.Employee", "TheHead")
                        .WithMany()
                        .HasForeignKey("HeadId");

                    b.HasOne("ErgoFab.Model.Organization", "TheOrganization")
                        .WithMany("TheDepartments")
                        .HasForeignKey("OrganizationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ErgoFab.Model.Department", "Parent")
                        .WithMany()
                        .HasForeignKey("ParentId");

                    b.Navigation("Parent");

                    b.Navigation("TheHead");

                    b.Navigation("TheOrganization");
                });

            modelBuilder.Entity("ErgoFab.Model.Employee", b =>
                {
                    b.HasOne("ErgoFab.Model.Country", "TheCountry")
                        .WithMany()
                        .HasForeignKey("CountryId");

                    b.HasOne("ErgoFab.Model.Department", "TheDepartment")
                        .WithMany("TheEmployees")
                        .HasForeignKey("DepartmentId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ErgoFab.Model.Organization", "TheOrganization")
                        .WithMany("TheEmployees")
                        .HasForeignKey("OrganizationId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.OwnsOne("ErgoFab.Model.Duration", "TheEnrollment", b1 =>
                        {
                            b1.Property<int>("EmployeeEmpId")
                                .HasColumnType("int");

                            b1.Property<DateTimeOffset?>("Finish")
                                .HasColumnType("datetimeoffset(2)");

                            b1.Property<DateTimeOffset>("Start")
                                .HasColumnType("datetimeoffset(2)");

                            b1.HasKey("EmployeeEmpId");

                            b1.ToTable("Employee");

                            b1.WithOwner()
                                .HasForeignKey("EmployeeEmpId");
                        });

                    b.Navigation("TheCountry");

                    b.Navigation("TheDepartment");

                    b.Navigation("TheEnrollment")
                        .IsRequired();

                    b.Navigation("TheOrganization");
                });

            modelBuilder.Entity("ErgoFab.Model.Occupation", b =>
                {
                    b.HasOne("ErgoFab.Model.Employee", "Employee")
                        .WithMany()
                        .HasForeignKey("EmployeeId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ErgoFab.Model.Project", "TheProject")
                        .WithMany("Employees")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Employee");

                    b.Navigation("TheProject");
                });

            modelBuilder.Entity("ErgoFab.Model.Organization", b =>
                {
                    b.HasOne("ErgoFab.Model.Country", "TheCountry")
                        .WithMany()
                        .HasForeignKey("CountryId");

                    b.HasOne("ErgoFab.Model.Employee", "TheDirector")
                        .WithMany()
                        .HasForeignKey("DirectorId");

                    b.Navigation("TheCountry");

                    b.Navigation("TheDirector");
                });

            modelBuilder.Entity("ErgoFab.Model.Project", b =>
                {
                    b.HasOne("ErgoFab.Model.Customer", "TheCustomer")
                        .WithMany()
                        .HasForeignKey("IdCustomer")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.OwnsOne("ErgoFab.Model.Duration", "TheProjectDuration", b1 =>
                        {
                            b1.Property<int>("ProjectId")
                                .HasColumnType("int");

                            b1.Property<DateTimeOffset?>("Finish")
                                .HasColumnType("datetimeoffset(2)");

                            b1.Property<DateTimeOffset>("Start")
                                .HasColumnType("datetimeoffset(2)");

                            b1.HasKey("ProjectId");

                            b1.ToTable("Project");

                            b1.WithOwner()
                                .HasForeignKey("ProjectId");
                        });

                    b.Navigation("TheCustomer");

                    b.Navigation("TheProjectDuration")
                        .IsRequired();
                });

            modelBuilder.Entity("ExpertIndustry", b =>
                {
                    b.HasOne("ErgoFab.Model.Expert", null)
                        .WithMany()
                        .HasForeignKey("TheExpertsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ErgoFab.Model.Industry", null)
                        .WithMany()
                        .HasForeignKey("TheIndustriesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("ErgoFab.Model.RegionalDivision", b =>
                {
                    b.HasOne("ErgoFab.Model.Organization", null)
                        .WithOne()
                        .HasForeignKey("ErgoFab.Model.RegionalDivision", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("ErgoFab.Model.Organization", "ParentOrganization")
                        .WithMany("TheSubDivisions")
                        .HasForeignKey("IdParent")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("ParentOrganization");
                });

            modelBuilder.Entity("ErgoFab.Model.Department", b =>
                {
                    b.Navigation("TheEmployees");
                });

            modelBuilder.Entity("ErgoFab.Model.Organization", b =>
                {
                    b.Navigation("TheDepartments");

                    b.Navigation("TheEmployees");

                    b.Navigation("TheSubDivisions");
                });

            modelBuilder.Entity("ErgoFab.Model.Project", b =>
                {
                    b.Navigation("Employees");
                });
#pragma warning restore 612, 618
        }
    }
}
