using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace PXF_file_merger
{
	public class Program
	{
		[STAThread] // required by System.Windows.Forms

		static void Main(string[] args){ Console.SetBufferSize(600,600);
			begin:
			Console.BackgroundColor= ConsoleColor.Black;
			Console.ForegroundColor= ConsoleColor.White;
			Console.Clear();

			Console.WriteLine("PixelForge .PXF font file merger\n      (c)2026 Ivan Stoilov");
			Console.WriteLine(                                  "********************************\n\nIf a glyph exists in only one of the two files, it will be automatically added to the final file.\nUse invalid input to reset file choices.\n\n");

			string dirPath= (Directory.GetCurrentDirectory()).ToString()+"\\files"; // set work path

			string[] dir= Directory.GetFileSystemEntries(dirPath);
			for(uint c=0;c<dir.Length;c++){ // Will trigger only if there are 1 or more elements.
				Console.BackgroundColor= ConsoleColor.White;
				Console.ForegroundColor= ConsoleColor.Black;	Console.Write(c+1);

				Console.BackgroundColor= ConsoleColor.Black;
				Console.ForegroundColor= ConsoleColor.White;	Console.WriteLine(". "+dir[c].Substring(dirPath.Length+1)+"\n");
			}
			Console.ForegroundColor= ConsoleColor.White;

			if(dir.Length==0){Console.Write("No files to list. Copy 2 or more files to the \"files\" directory, and press any key to refresh the list..."); Console.ReadKey(); goto begin;}else if(dir.Length<2){Console.Write("A minimum of 2 or more files must be present in the \"files\" directory. Press any key to refresh the list..."); Console.ReadKey(); goto begin;}

			
			uint[] fileselection= new uint[2];
			try{

				Console.Write("Select first file: "); string cmd_input= Console.ReadLine();
				if(uint.Parse(cmd_input)>dir.Length || uint.Parse(cmd_input)==0){goto begin;}
				fileselection[0]=(uint.Parse(cmd_input)-1);
				
				Console.Write("Select second file: "); cmd_input= Console.ReadLine();
				if(uint.Parse(cmd_input)>dir.Length || uint.Parse(cmd_input)==0){goto begin;}
				fileselection[1]=(uint.Parse(cmd_input)-1);

			}catch{goto begin;}


			Console.Clear();
			
			Console.WriteLine("Buffering files...");

			string file1= "";
			using (var streamReader = new StreamReader(dir[fileselection[0]]))
			{
			file1= streamReader.ReadToEnd();
			}
			
			string file2= "";
			using (var streamReader = new StreamReader(dir[fileselection[1]]))
			{
			file2= streamReader.ReadToEnd();
			}
			
			string[] file1work_tmp= file1.Split('\n');
			List<string>file1work= new List<string>(); for(int c=0;c<file1work_tmp.Length;c++){file1work.Add(file1work_tmp[c]);}
			string[] file2work_tmp= file2.Split('\n');
			List<string>file2work= new List<string>(); for(int c=0;c<file2work_tmp.Length;c++){file2work.Add(file2work_tmp[c]);}
			
			file1="";
			file2="";
			string file3_header= "";
			string file3_glyphcount_start= "num_glyphs: "; string file3_glyphcount_end= "\r\n";
			string file3_data= "glyphs:\r\n";

			Console.WriteLine("Buffer OK.\n");

			Console.WriteLine("Reading headers...");
			for(ushort c=0;c<17;c++){
				if(file1work[c]!=file2work[c]){
					Console.ForegroundColor=ConsoleColor.Yellow; Console.WriteLine("Found difference in header.\n\n"); Console.ForegroundColor=ConsoleColor.White;
					Console.BackgroundColor=ConsoleColor.DarkGray; Console.Write("1."); Console.BackgroundColor=ConsoleColor.Black;
					Console.WriteLine(" "+(dir[fileselection[0]].Substring(dirPath.Length+1))+":");
					Console.BackgroundColor=ConsoleColor.White; Console.ForegroundColor=ConsoleColor.Black;
					Console.WriteLine(file1work[c]);
					Console.WriteLine();

					Console.ForegroundColor=ConsoleColor.White;
					Console.BackgroundColor=ConsoleColor.DarkGray; Console.Write("2."); Console.BackgroundColor=ConsoleColor.Black;
					Console.WriteLine(" "+(dir[fileselection[1]].Substring(dirPath.Length+1))+":");
					Console.BackgroundColor=ConsoleColor.White; Console.ForegroundColor=ConsoleColor.Black;
					Console.WriteLine(file2work[c]);
					Console.WriteLine();
					
					Console.BackgroundColor=ConsoleColor.Black; Console.ForegroundColor=ConsoleColor.White;
					Console.WriteLine("Press 1 or 2 to select a line for the final file (0 to abort):");
					{
					string cmd_input="";
					for(;(cmd_input!="1" && cmd_input!="2") && cmd_input!="0";){
					cmd_input= Console.ReadKey(true).KeyChar.ToString();
					}
					if(cmd_input=="1"){file3_header+=file1work[c]+"\n";}else
					if(cmd_input=="0"){goto begin;}else{file3_header+=file2work[c]+"\n";}
					Console.WriteLine(cmd_input+" - OK\n");
					}
				}else{file3_header+=file1work[c]+"\n";}
			}

			for(ushort c=0;c<19;c++){file1work.RemoveAt(0); file2work.RemoveAt(0);} // prepare for operation with glyphs: trim header part
			Console.WriteLine("Headers OK.\n");

			UInt16 num_glyphs=0;
			uint preview_horizstretch=2;

			uint added_glyphs1=0, added_glyphs2=0, differences=0, existing_glyphs=0;
			
			List<UInt16> file3_log_differences= new List<UInt16>();
			List<bool> file3_log_differences_choices_Pixels= new List<bool>();
			List<bool> file3_log_differences_choices_Advance= new List<bool>();
			List<bool> file3_log_differences_choices_AutoAdvance= new List<bool>();
			List<bool> file3_log_differences_choices_AutoAdvanceAmount= new List<bool>();
			
			List<bool> file3_log_differences_choices_Pixels_manual= new List<bool>();
			List<bool> file3_log_differences_choices_Advance_manual= new List<bool>();
			List<bool> file3_log_differences_choices_AutoAdvance_manual= new List<bool>();
			List<bool> file3_log_differences_choices_AutoAdvanceAmount_manual= new List<bool>();
			
			List<UInt16> file3_log_added1= new List<UInt16>();
			List<UInt16> file3_log_added2= new List<UInt16>();
			List<UInt16> file3_log_existing= new List<UInt16>();


			bool[] bookmark_chars= new bool[ushort.MaxValue+1];
			uint num_bookmarks=0; // max amount is 65536, not 65535


			Console.Clear();
			Console.WriteLine("Reading glyphs...");
			// scan chars
			{
			UInt16 counter=0;
			do{


				// check existence of glyph in both files.
				bool glyph1exists=false; bool glyph2exists=false;
				
				int c1=0; for(;c1<file1work.Count;c1+=5){
					if(("\t"+counter+":\r")==file1work[c1]){glyph1exists=true; break;}
				}

				int c2=0; for(;c2<file2work.Count;c2+=5){
					if(("\t"+counter+":\r")==file2work[c2]){glyph2exists=true; break;}
				}



				if(glyph1exists==true && glyph2exists==true){ file3_log_existing.Add(counter);
					file3_data+=file1work[c1]+"\n";
					if(file1work[c1+1]!=file2work[c2+1] || file1work[c1+2]!=file2work[c2+2] || file1work[c1+3]!=file2work[c2+3] || file1work[c1+4]!=file2work[c2+4]){ // check if glyph or its info are different

						Console.CursorVisible= false;

						// true = difference
						bool advance_Current_isdifferent= (file1work[c1+1]!=file2work[c2+1]);
						bool auto_update_advance_Current_isdifferent= (file1work[c1+2]!=file2work[c2+2]);
						bool auto_advance_amount_Current_isdifferent= (file1work[c1+3]!=file2work[c2+3]);
						bool pixelgrid_isdifferent= (file1work[c1+4]!=file2work[c2+4]);


						//UInt32[] advance_Current= new UInt32[2];
						//Boolean[] auto_update_advance_Current= new Boolean[2];
						//UInt32[] auto_advance_amount_Current= new UInt32[2];

						// define pixel grid
						bool[][][] glyphdata= new bool[2][][];
						glyphdata[0]= new bool[64][]; // lines 1
						for(ushort c=0;c<64;c++){glyphdata[0][c]=new bool[64];} // columns 1
						glyphdata[1]= new bool[64][]; // lines 2
						for(ushort c=0;c<64;c++){glyphdata[1][c]=new bool[64];} // columns 2



						
						string file1coords= (file1work[c1+4].Substring(10));
						file1coords= file1coords.Remove(file1coords.Length-2);
						string[] file1coords_split= file1coords.Split(',');
						for(uint c=0;c<file1coords_split.Length;c++){file1coords_split[c]= file1coords_split[c].TrimStart(' ');}
						
						string file2coords= (file2work[c2+4].Substring(10));
						file2coords= file2coords.Remove(file2coords.Length-2);
						string[] file2coords_split= file2coords.Split(',');
						for(uint c=0;c<file2coords_split.Length;c++){file2coords_split[c]= file2coords_split[c].TrimStart(' ');}
						


						// read pixels and store in the arrays
						for(uint c=0;c<file1coords_split.Length-1;c++){
							glyphdata[0][63-(int.Parse((file1coords_split[c].Split(' '))[1])+32)][int.Parse((file1coords_split[c].Split(' '))[0])+32] = true;
						}

						for(uint c=0;c<file2coords_split.Length-1;c++){
							glyphdata[1][63-(int.Parse((file2coords_split[c].Split(' '))[1])+32)][int.Parse((file2coords_split[c].Split(' '))[0])+32] = true;
						}

						

						

						{

						string[] glyphpromptselections= new string[4],
						         glyphpromptselections_display= new string[4]{" ", " ", " ", " "}
						;

						for(ushort currentchar=0, glyphprompt_currentSelection=0 ; ; ){ // display cmp prompt
							string cmd_input="";
							if( (glyphprompt_currentSelection==0 && pixelgrid_isdifferent) || (glyphprompt_currentSelection==1 && advance_Current_isdifferent) || (glyphprompt_currentSelection==2 && auto_update_advance_Current_isdifferent) || (glyphprompt_currentSelection==3 && auto_advance_amount_Current_isdifferent) ){

							Console.Clear();
							Console.ForegroundColor=ConsoleColor.Yellow; Console.WriteLine("Found difference in this glyph's pixels and/or \"Advance\" properties.\n"); Console.ForegroundColor=ConsoleColor.White;
							
							Console.ForegroundColor=ConsoleColor.Cyan; Console.Write("U+"+(counter.ToString("X4"))+" ("+counter+")");



							Console.ForegroundColor=ConsoleColor.DarkGray;	Console.Write(" - Choices: { Pixels: ");
							Console.ForegroundColor=ConsoleColor.White;		Console.Write(glyphpromptselections_display[3]);
							
							Console.ForegroundColor=ConsoleColor.DarkGray;	Console.Write(", Advance: ");
							if(glyphprompt_currentSelection==1){Console.SetCursorPosition(Console.CursorLeft,Console.CursorTop+1); Console.BackgroundColor=ConsoleColor.Red; Console.ForegroundColor=ConsoleColor.White; Console.Write("^"); Console.SetCursorPosition(Console.CursorLeft-1,Console.CursorTop-1); Console.BackgroundColor=ConsoleColor.Black;}
							Console.ForegroundColor=ConsoleColor.White;		Console.Write(glyphpromptselections_display[0]);
							
							Console.ForegroundColor=ConsoleColor.DarkGray;	Console.Write(", AutoUpdateAdvance: ");
							if(glyphprompt_currentSelection==2){Console.SetCursorPosition(Console.CursorLeft,Console.CursorTop+1); Console.BackgroundColor=ConsoleColor.Red; Console.ForegroundColor=ConsoleColor.White; Console.Write("^"); Console.SetCursorPosition(Console.CursorLeft-1,Console.CursorTop-1); Console.BackgroundColor=ConsoleColor.Black;}
							Console.ForegroundColor=ConsoleColor.White;		Console.Write(glyphpromptselections_display[1]);
							
							Console.ForegroundColor=ConsoleColor.DarkGray;	Console.Write(", AutoUpdateAmount: ");
							if(glyphprompt_currentSelection==3){Console.SetCursorPosition(Console.CursorLeft,Console.CursorTop+1); Console.BackgroundColor=ConsoleColor.Red; Console.ForegroundColor=ConsoleColor.White; Console.Write("^"); Console.SetCursorPosition(Console.CursorLeft-1,Console.CursorTop-1); Console.BackgroundColor=ConsoleColor.Black;}
							Console.ForegroundColor=ConsoleColor.White;		Console.Write(glyphpromptselections_display[2]);
							Console.ForegroundColor=ConsoleColor.DarkGray;	Console.WriteLine("} ");
							
							if(bookmark_chars[counter]){
								Console.BackgroundColor=ConsoleColor.White;
								Console.ForegroundColor=ConsoleColor.Black;
								Console.Write("*");
								Console.BackgroundColor= ConsoleColor.Black;
							}
							Console.WriteLine();
							
							Console.ForegroundColor=ConsoleColor.White;		Console.Write(currentchar+1);
							Console.ForegroundColor=ConsoleColor.DarkGray;	Console.Write("/2"+": ");
							Console.ForegroundColor=ConsoleColor.DarkYellow;		Console.WriteLine((dir[fileselection[currentchar]].Substring(dirPath.Length+1))+"\n");
							Console.ForegroundColor=ConsoleColor.White;

							string glyphpreview="";
							for(ushort c=0;c<64;c++){
								for(ushort d=0;d<64;d++){
									if(glyphdata[currentchar][c][d]==true){for(ushort e=0;e<preview_horizstretch;e++){glyphpreview+="█";}}else{for(ushort e=0;e<preview_horizstretch;e++){if((c&1)==1){if((d&1)==0){glyphpreview+=".";}else{glyphpreview+=" ";}}else{glyphpreview+=" ";}}}
								} glyphpreview+="\n";
							}
							if(!pixelgrid_isdifferent){Console.ForegroundColor=ConsoleColor.DarkCyan;}
							Console.Write(glyphpreview); Console.ForegroundColor=ConsoleColor.White;


							Console.SetCursorPosition(0,Console.CursorTop-1); // last line of glyph preview is not needed.
							{
							if(!advance_Current_isdifferent){Console.ForegroundColor=ConsoleColor.DarkCyan;}
							string advance_render="";
							if(currentchar==0){
								advance_render= file1work[c1+1].Substring(11);
								advance_render= advance_render.Remove(advance_render.Length-1);
							}else{
								advance_render= file2work[c2+1].Substring(11);
								advance_render= advance_render.Remove(advance_render.Length-1);
							} Console.Write("Advance = "+advance_render);
							Console.ForegroundColor=ConsoleColor.White;
							}
							

							{
							if(!auto_update_advance_Current_isdifferent){Console.ForegroundColor=ConsoleColor.DarkCyan;}
							string advance_render="";
							if(currentchar==0){
								advance_render= file1work[c1+2].Substring(23);
								advance_render= advance_render.Remove(advance_render.Length-1);
							}else{
								advance_render= file2work[c2+2].Substring(23);
								advance_render= advance_render.Remove(advance_render.Length-1);
							} Console.Write("    | Auto Update Advance = "+advance_render.ToUpper());
							Console.ForegroundColor=ConsoleColor.White;
							}


							{
							if(!auto_advance_amount_Current_isdifferent){Console.ForegroundColor=ConsoleColor.DarkCyan;}
							string advance_render="";
							if(currentchar==0){
								advance_render= file1work[c1+3].Substring(23);
								advance_render= advance_render.Remove(advance_render.Length-1);
							}else{
								advance_render= file2work[c2+3].Substring(23);
								advance_render= advance_render.Remove(advance_render.Length-1);
							} Console.WriteLine("    | Auto Update Amount = "+advance_render);
							Console.ForegroundColor=ConsoleColor.White;
							}

							
							Console.ForegroundColor=ConsoleColor.DarkGray;
							Console.Write("\n<- and -> - nav diffs, Enter - keep selected property, B/F - toggle bookmark\n1-9 - stretch preview horizontally (Stretch = "+preview_horizstretch+")\nEsc - abort and reset program");
							Console.ForegroundColor=ConsoleColor.White;
							

							// place helpers
							if(glyphprompt_currentSelection==0){
							Console.BackgroundColor=ConsoleColor.Red;
							Console.SetCursorPosition((((int)(preview_horizstretch))*32),5); for(ushort c=0;c<preview_horizstretch;c++){Console.Write("V");}
							Console.SetCursorPosition((((int)(preview_horizstretch))*64),(5+32)); for(ushort c=0;c<preview_horizstretch;c++){Console.Write("<");}
							Console.SetCursorPosition((((int)(preview_horizstretch))*32),(5+32)); 
							if(glyphdata[currentchar][31][32]==true){
								Console.BackgroundColor=ConsoleColor.Cyan;}else{Console.BackgroundColor=ConsoleColor.Red;}
							for(ushort c=0;c<preview_horizstretch;c++){Console.Write(">");}
							Console.BackgroundColor=ConsoleColor.Black;
							}


							Console.SetCursorPosition(0,0);
							cmd_input= Console.ReadKey(true).Key.ToString();

							}else{cmd_input="Enter";}
							
							for(ushort c=1;c<10;c++){if(cmd_input=="D"+c){preview_horizstretch=c; break;}}
							if(cmd_input=="LeftArrow"){if(currentchar>0){currentchar--;}}
							if(cmd_input=="RightArrow"){if(currentchar<1){currentchar++;}}
							if(cmd_input=="Escape"){
								Console.BackgroundColor=ConsoleColor.White;
								Console.ForegroundColor=ConsoleColor.Black;		Console.Write("Really abort and reset? Y/N");

								for(string cmd_input1="";cmd_input1!= "y" && cmd_input1!="n";){
								cmd_input1= Console.ReadKey(true).KeyChar.ToString().ToLower();
								if(cmd_input1=="y"){goto begin;}
								}
								Console.BackgroundColor=ConsoleColor.Black;
								Console.ForegroundColor=ConsoleColor.White;
							}
							if(cmd_input=="B" || cmd_input=="F"){
								bookmark_chars[counter]= !bookmark_chars[counter]; // complement
								if(bookmark_chars[counter]){num_bookmarks++;}else{num_bookmarks--;}
							}
							if(cmd_input=="Enter"){
								Console.Clear();
								if(currentchar==0){
									if(glyphprompt_currentSelection==0){
										glyphpromptselections[3]= file1work[c1+4]+"\n";
										glyphpromptselections_display[3]= "1";
									} else
									if(glyphprompt_currentSelection==1){
										glyphpromptselections[0]= file1work[c1+1]+"\n";
										glyphpromptselections_display[0]= "1";
									} else
									if(glyphprompt_currentSelection==2){
										glyphpromptselections[1]= file1work[c1+2]+"\n";
										glyphpromptselections_display[1]= "1";
									} else
									if(glyphprompt_currentSelection==3){
										differences++; file3_log_differences.Add(counter);

										glyphpromptselections[2]= file1work[c1+3]+"\n";
										glyphpromptselections_display[2]= "1";
										
										file3_data+=glyphpromptselections[0]+glyphpromptselections[1]+glyphpromptselections[2]+glyphpromptselections[3];

										Console.CursorVisible= true; Console.WriteLine("Reading glyphs..."); break;
									}
									
									glyphprompt_currentSelection++;
									
									//file3_log_differences_choices_Pixels.Add(false);
									//file3_log_differences_choices_Advance.Add(false);
									//file3_log_differences_choices_AutoAdvance.Add(false);
									//file3_log_differences_choices_AutoAdvanceAmount.Add(false);
								} else
								if(currentchar==1){
									if(glyphprompt_currentSelection==0){
										glyphpromptselections[3]= file2work[c2+4]+"\n";
										glyphpromptselections_display[3]= "2";
									} else
									if(glyphprompt_currentSelection==1){
										glyphpromptselections[0]= file2work[c2+1]+"\n";
										glyphpromptselections_display[0]= "2";
									} else
									if(glyphprompt_currentSelection==2){
										glyphpromptselections[1]= file2work[c2+2]+"\n";
										glyphpromptselections_display[1]= "2";
									} else
									if(glyphprompt_currentSelection==3){
										differences++; file3_log_differences.Add(counter);
										
										glyphpromptselections[2]= file2work[c2+3]+"\n";
										
										file3_data+=glyphpromptselections[0]+glyphpromptselections[1]+glyphpromptselections[2]+glyphpromptselections[3];

										Console.Clear(); Console.WriteLine("Reading glyphs..."); break;
									}
									
									glyphprompt_currentSelection++;

									//file3_log_differences_choices_Pixels.Add(true);
									//file3_log_differences_choices_Advance.Add(true);
									//file3_log_differences_choices_AutoAdvance.Add(true);
									//file3_log_differences_choices_AutoAdvanceAmount.Add(true);
								}
								
								
							}

						}



					}

					} else{file3_data+=file1work[c1+1]+"\n"+file1work[c1+2]+"\n"+file1work[c1+3]+"\n"+file1work[c1+4]+"\n";}


					num_glyphs++; existing_glyphs++;

				}else if(glyph1exists==true){
					file3_data+=file1work[c1]+"\n"+file1work[c1+1]+"\n"+file1work[c1+2]+"\n"+file1work[c1+3]+"\n"+file1work[c1+4]+"\n"; // automatically add glyph
					num_glyphs++; added_glyphs1++;

					file3_log_added1.Add(counter);

				}else if(glyph2exists==true){
					file3_data+=file2work[c2]+"\n"+file2work[c2+1]+"\n"+file2work[c2+2]+"\n"+file2work[c2+3]+"\n"+file2work[c2+4]+"\n"; // automatically add glyph
					num_glyphs++; added_glyphs2++;

					file3_log_added2.Add(counter);
				}




				counter++;
			}while(counter>0);
			
			} // close scope
			

			string file3= file3_header+file3_glyphcount_start+num_glyphs+file3_glyphcount_end+file3_data;
			string file3_log= "";


			for(;;){
				Console.Clear();
				Console.WriteLine("Glyphs OK.\n");

																Console.Write("Total glyphs: ");
				Console.ForegroundColor=ConsoleColor.Yellow;	Console.WriteLine(num_glyphs);
				Console.ForegroundColor=ConsoleColor.White;		Console.WriteLine("    of which:");
																Console.Write("        Overlapping glyphs: ");
				Console.ForegroundColor=ConsoleColor.Yellow;	Console.WriteLine(existing_glyphs);
				Console.ForegroundColor=ConsoleColor.White;		Console.Write("        Added glyphs: ");
				Console.ForegroundColor=ConsoleColor.Yellow;	Console.Write(added_glyphs1);
				Console.ForegroundColor=ConsoleColor.White;		Console.Write(" + ");
				Console.ForegroundColor=ConsoleColor.Yellow;	Console.Write(added_glyphs2);
				Console.ForegroundColor=ConsoleColor.White;		Console.Write(" = ");
				Console.ForegroundColor=ConsoleColor.Yellow;	Console.WriteLine((added_glyphs1+added_glyphs2));
				Console.ForegroundColor=ConsoleColor.White;		Console.Write("        Different glyphs: ");
				Console.ForegroundColor=ConsoleColor.Yellow;	Console.WriteLine(differences);
				
				Console.ForegroundColor=ConsoleColor.White;		Console.Write("\nYou bookmarked ");
				Console.ForegroundColor=ConsoleColor.Yellow;	Console.Write(num_bookmarks);
				Console.ForegroundColor=ConsoleColor.White;		Console.WriteLine(" glyphs.");
				
				Console.WriteLine();

				Console.BackgroundColor=ConsoleColor.White; Console.ForegroundColor=ConsoleColor.Black;
				Console.Write("C"); Console.BackgroundColor=ConsoleColor.Black; Console.ForegroundColor=ConsoleColor.White;
				Console.Write("opy to clipboard, ");
				Console.BackgroundColor=ConsoleColor.White; Console.ForegroundColor=ConsoleColor.Black;
				Console.Write("W"); Console.BackgroundColor=ConsoleColor.Black; Console.ForegroundColor=ConsoleColor.White;
				Console.Write("rite to file, ");
				Console.BackgroundColor=ConsoleColor.White; Console.ForegroundColor=ConsoleColor.Black;
				Console.Write("L"); Console.BackgroundColor=ConsoleColor.Black; Console.ForegroundColor=ConsoleColor.White;
				Console.Write("og bookmarks to file or ");
				Console.BackgroundColor=ConsoleColor.White; Console.ForegroundColor=ConsoleColor.Black;
				Console.Write("R"); Console.BackgroundColor=ConsoleColor.Black; Console.ForegroundColor=ConsoleColor.White;
				Console.Write("eset?\nAll commands, except \"Reset\", will return you to this screen upon completion.");

				string cmd_input= Console.ReadKey(true).KeyChar.ToString().ToLower();
				if(cmd_input=="r"){goto begin;}else
				if(cmd_input=="c"){
					System.Windows.Forms.Clipboard.SetText(file3);
					Console.WriteLine("\n\n\nCopied to clipboard.\nPress any key to continue..."); Console.ReadKey();
				}else
				if(cmd_input=="w"){
					Console.WriteLine("\n\n\nFilename of merged file (without \".pxf\"):");
					string file3filename= Console.ReadLine();
					Console.SetCursorPosition(file3filename.Length,Console.CursorTop-1);
					Console.BackgroundColor=ConsoleColor.White; Console.ForegroundColor=ConsoleColor.Black;
					Console.WriteLine(".pxf\n");
					Console.BackgroundColor=ConsoleColor.Black; Console.ForegroundColor=ConsoleColor.White;

					Console.WriteLine("Writing...");
					using (var streamWriter = new StreamWriter(dirPath+"\\"+file3filename+".pxf"))
					{
						streamWriter.Write(file3);
					} Console.Write("\n\nWrote file.\nPress any key to continue..."); Console.ReadKey();
				}else
				if(cmd_input=="l"){
					string logfilename= "bookmarks_"+DateTime.Now.Year.ToString()+"-"+DateTime.Now.Month.ToString()+"-"+DateTime.Now.Day.ToString()+"_"+DateTime.Now.Hour.ToString()+"-"+DateTime.Now.Minute.ToString()+"-"+DateTime.Now.Second.ToString()+"-"+DateTime.Now.Millisecond.ToString()+".log";
					Console.WriteLine();

					string bookmark_output="";
					
					ushort c=0;
					do{
						if(bookmark_chars[c]){bookmark_output+= (c.ToString("X4")+" ("+c+") - "+"'"+((char)c)+"'\n");}
						c++;
					} while(c>0);
					
					using (var streamWriter = new StreamWriter(dirPath+"\\"+logfilename))
					{
						streamWriter.Write(bookmark_output);
					}
					Console.WriteLine("\n\nLogged bookmarks to log file.\n");
					Console.WriteLine(logfilename);
					Console.Write("\nPress any key to continue..."); Console.ReadKey();
				}
			
			
			}

			
		}
	}
}
