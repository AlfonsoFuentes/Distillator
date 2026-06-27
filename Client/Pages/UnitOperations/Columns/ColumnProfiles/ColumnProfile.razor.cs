using Client.Pages.UnitOperations.Columns.ColumnProfiles.ColumnComposition;
using Client.Pages.UnitOperations.Columns.ColumnProfiles.ColumnPlates;
using Client.Templates.Panels;
using Microsoft.AspNetCore.Components;
using Shared.SolverConsecutive.Equipments.Columns;

namespace Client.Pages.UnitOperations.Columns.ColumnProfiles
{

    public partial class ColumnProfile : ComponentBase
    {
        [Parameter] public SolverColumn? Column { get; set; }
        [Parameter] public bool IsLoading { get; set; } = false;

        private string _selectedNodeId = string.Empty;
        private List<ExplorerNode> _treeNodes = new();

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            BuildTree();
        }

        private void BuildTree()
        {
            if (Column == null) return;

            _treeNodes = new List<ExplorerNode>
            {
                new ExplorerNode
                {
                    Name = "By Concentration",
                    IsFolder = true,
                    Children = new List<ExplorerNode>
                    {
                        new ExplorerNode
                        {
                            Id = "mccabe-thiele",
                            Name = "McCabe-Thiele Diagram",
                            Content=  CreateMcCabeThieleContent()
                           
                        }    ,
                        new ExplorerNode
                        {
                            Id = "temperature-concentration",
                            Name = "Temperature vs x,y",
                            Content=  CreateTemperatureConcentration()

                        },
                        new ExplorerNode
                        {
                            Id = "pressure-concentration",
                            Name = "Pressure vs x,y",
                            Content=  CreatePressureConcentration()

                        }  ,
                        new ExplorerNode
                        {
                            Id = "enthalpy-concentration",
                            Name = "Enthalpy vs x,y",
                            Content=  CreateEnthalpyConcentration()

                        }
                    }
                },new ExplorerNode
                {
                    Name="By Plates"  ,
                    IsFolder = true,
                    Children=new List<ExplorerNode>
                    {
                        new ExplorerNode
                        {
                            Id="Temperature-plates",
                            Name="Temperature vs plates",
                            Content= CreateTemperaturePlates()
                        }  ,
                        new ExplorerNode
                        {
                            Id="pressure-plates",
                            Name="Pressure vs plates",
                            Content= CreatePressurePlates()
                        },
                        new ExplorerNode
                        {
                            Id="enthalpy-plates",
                            Name="Enthlapy vs plates",
                            Content= CreateEnthalpyPlates()
                        }
                    }
                }

            };
        }

        // 🔥 Método auxiliar que crea el RenderFragment
        private RenderFragment CreateMcCabeThieleContent()
        {
            return builder =>
            {
                builder.OpenComponent<McCabeThieleChart>(0);
                builder.AddAttribute(1, nameof(McCabeThieleChart.Column), Column);
            
                builder.CloseComponent();
            };
        }
        private RenderFragment CreateTemperaturePlates()
        {
            return builder =>
            {
                builder.OpenComponent<TemperatureProfileChart>(0);
                builder.AddAttribute(1, nameof(TemperatureProfileChart.Column), Column);

                builder.CloseComponent();
            };
        }
        private RenderFragment CreatePressurePlates()
        {
            return builder =>
            {
                builder.OpenComponent<PressureProfileChart>(0);
                builder.AddAttribute(1, nameof(PressureProfileChart.Column), Column);

                builder.CloseComponent();
            };
        }
        private RenderFragment CreateEnthalpyPlates()
        {
            return builder =>
            {
                builder.OpenComponent<EnthalpyProfileChart>(0);
                builder.AddAttribute(1, nameof(EnthalpyProfileChart.Column), Column);

                builder.CloseComponent();
            };
        }
        private RenderFragment CreateTemperatureConcentration()
        {
            return builder =>
            {
                builder.OpenComponent<TemperatureCompositionChart>(0);
                builder.AddAttribute(1, nameof(TemperatureCompositionChart.Column), Column);

                builder.CloseComponent();
            };
        }
        private RenderFragment CreatePressureConcentration()
        {
            return builder =>
            {
                builder.OpenComponent<PressureCompositionChart>(0);
                builder.AddAttribute(1, nameof(PressureCompositionChart.Column), Column);

                builder.CloseComponent();
            };
        }
        private RenderFragment CreateEnthalpyConcentration()
        {
            return builder =>
            {
                builder.OpenComponent<EnthalpyCompositionChart>(0);
                builder.AddAttribute(1, nameof(EnthalpyCompositionChart.Column), Column);

                builder.CloseComponent();
            };
        }

        private void HandleNodeSelected(string nodeId)
        {
            _selectedNodeId = nodeId;
            StateHasChanged();
        }
    }
}
