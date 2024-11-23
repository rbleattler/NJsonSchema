﻿using NJsonSchema.Generation.TypeMappers;
using NJsonSchema.NewtonsoftJson.Generation;
using Xunit;

namespace NJsonSchema.Tests.Generation
{
    public class TypeMapperTests
    {
        public class Foo
        {
            public Bar Bar1 { get; set; }

            public Bar Bar2 { get; set; }
        }

        public class Bar
        {

        }

        [Fact]
        public async Task When_primitive_type_mapping_is_available_for_type_then_it_is_called()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                TypeMappers =
                {
                    new PrimitiveTypeMapper(typeof(Bar), s => s.Type = JsonObjectType.String)
                }
            });

            // Assert
            var json = schema.ToJson();
            var property = schema.Properties["Bar1"].ActualTypeSchema;

            Assert.True(property.Type.HasFlag(JsonObjectType.String));
            Assert.DoesNotContain("$ref", json);
        }

        [Fact]
        public async Task When_object_type_mapping_is_available_for_type_then_it_is_called()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<Foo>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                TypeMappers =
                {
                    new ObjectTypeMapper(typeof(Bar), new JsonSchema
                        {
                            Type = JsonObjectType.Object,
                            Properties =
                            {
                                {
                                    "Prop",
                                    new JsonSchemaProperty
                                    {
                                        IsRequired = true,
                                        Type = JsonObjectType.String
                                    }
                                }
                            }
                        }
                    )
                }
            });

            // Assert
            var json = schema.ToJson();

            var property1 = schema.Properties["Bar1"];
            var property2 = schema.Properties["Bar2"];

            Assert.True(property1.ActualTypeSchema.Properties.ContainsKey("Prop"));
            Assert.True(property1.ActualTypeSchema == property2.ActualTypeSchema);

            Assert.Contains("$ref", json);
        }



        public class MyFoo
        {
            public MyWrapper<MyBar> Property { get; set; }
        }

        public class MyWrapper<T>
        {

        }

        public class MyBar
        {
            public string Name { get; set; }
        }

        public class MyTypeMapper : ITypeMapper
        {
            public Type MappedType => typeof(MyWrapper<>);

            public bool UseReference => true;

            public void GenerateSchema(JsonSchema schema, TypeMapperContext context)
            {
                schema.Reference = context.JsonSchemaGenerator.Generate(context.Type.GenericTypeArguments[0], context.JsonSchemaResolver);
            }
        }

        [Fact]
        public async Task When_generic_type_mapper_is_defined_then_it_is_called_and_the_refs_are_correct()
        {
            // Act
            var schema = NewtonsoftJsonSchemaGenerator.FromType<MyFoo>(new NewtonsoftJsonSchemaGeneratorSettings
            {
                TypeMappers =
                {
                    new MyTypeMapper()
                }
            });

            // Assert
            var json = schema.ToJson();
            Assert.True(schema.Definitions.ContainsKey("MyBar"));
            Assert.False(schema.Definitions.ContainsKey("MyWrapperOfMyBar"));
        }

    }
}
